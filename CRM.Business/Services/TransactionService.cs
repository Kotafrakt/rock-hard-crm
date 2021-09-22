﻿using CRM.Business.Constants;
using CRM.Business.Exceptions;
using CRM.Business.IdentityInfo;
using CRM.Business.Models;
using CRM.Business.Requests;
using CRM.Business.ValidationHelpers;
using CRM.Core;
using CRM.DAL.Enums;
using CRM.DAL.Models;
using CRM.DAL.Repositories;
using MailExchange;
using MassTransit;
using Microsoft.Extensions.Options;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Business.Serialization;
using static CRM.Business.Constants.TransactionEndpoint;

namespace CRM.Business.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly RestClient _client;
        private readonly RequestHelper _requestHelper;
        private readonly IAccountValidationHelper _accountValidationHelper;
        private readonly IAccountService _accountService;
        private readonly ICommissionFeeService _commissionFeeService;
        private readonly decimal _commission;
        private readonly decimal _vipCommission;
        private readonly decimal _commissionModifier;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILeadRepository _leadRepository;

        public TransactionService
        (
            IOptions<CommissionSettings> commissionOptions,
            IOptions<ConnectionSettings> connectionOptions,
            IAccountValidationHelper accountValidationHelper,
            IAccountService accountService,
            ICommissionFeeService commissionFeeService,
            IPublishEndpoint publishEndpoint,
            ILeadRepository leadRepository,
            RestClient restClient
        )
        {
            _client = new RestClient(connectionOptions.Value.TransactionStoreUrl);
            _client.AddHandler("application/json", () => NewtonsoftJsonSerializer.Default);
            _commission = commissionOptions.Value.Commission;
            _vipCommission = commissionOptions.Value.VipCommission;
            _commissionModifier = commissionOptions.Value.CommissionModifier;
            _requestHelper = new RequestHelper();
            _accountValidationHelper = accountValidationHelper;
            _accountService = accountService;
            _commissionFeeService = commissionFeeService;
            _publishEndpoint = publishEndpoint;
            _leadRepository = leadRepository;
        }

        public async Task<CommissionFeeDto> AddDepositAsync(TransactionBusinessModel model, LeadIdentityInfo leadInfo)
        {
            var leadDto = await _leadRepository.GetLeadByIdAsync(leadInfo.LeadId);
            var account = await CheckAccessAndReturnAccount(model.AccountId, leadInfo);
            _accountValidationHelper.CheckForVipAccess(account.Currency, leadInfo);
            var commission = CalculateCommission(model.Amount, leadInfo);

            model.Amount -= commission;
            model.AccountId = account.Id;
            model.Currency = account.Currency;

            var request = _requestHelper.CreatePostRequest(AddDepositEndpoint, model);
            var result = _client.Execute<long>(request);
            var transactionId = result.Data;

            EmailSender(leadDto, EmailMessages.DepositSubject, string.Format(EmailMessages.DepositBody, model.Amount));
            var dto = new CommissionFeeDto
            {
                LeadId = leadInfo.LeadId,
                AccountId = model.AccountId,
                TransactionId = transactionId,
                Role = leadInfo.Role,
                CommissionAmount = commission,
                TransactionType = TransactionType.Deposit
            };

            dto.Id = await AddCommissionFee(dto);

            return dto;
        }

        public async Task<CommissionFeeDto> AddWithdrawAsync(TransactionBusinessModel model, LeadIdentityInfo leadInfo)
        {
            var leadDto = await _leadRepository.GetLeadByIdAsync(leadInfo.LeadId);
            var accountModel = await _accountService.GetAccountWithTransactionsAsync(model.AccountId, leadInfo);
            _accountValidationHelper.CheckForVipAccess(accountModel.Currency, leadInfo);
            var commission = CalculateCommission(model.Amount, leadInfo);
            _accountValidationHelper.CheckBalance(accountModel, model.Amount);
            model.Date = _accountValidationHelper.GetTransactionsLastDateAndThrowIfNotFound(accountModel);

            model.Amount -= commission;
            model.AccountId = accountModel.Id;
            model.Currency = accountModel.Currency;

            var request = _requestHelper.CreatePostRequest(AddWithdrawEndpoint, model);
            var result = _client.Execute<long>(request);
            var transactionId = result.Data;

            _accountValidationHelper.CheckForDuplicateTransaction(transactionId,accountModel);

            EmailSender(leadDto, EmailMessages.WithdrawSubject, string.Format(EmailMessages.WithdrawBody, model.Amount));

            var dto = new CommissionFeeDto
            {
                LeadId = leadInfo.LeadId,
                AccountId = model.AccountId,
                TransactionId = transactionId,
                Role = leadInfo.Role,
                CommissionAmount = commission,
                TransactionType = TransactionType.Withdraw
            };

            dto.Id = await AddCommissionFee(dto);

            return dto;
        }

        public async Task<CommissionFeeDto> AddTransferAsync(TransferBusinessModel model, LeadIdentityInfo leadInfo)
        {
            var leadDto = await _leadRepository.GetLeadByIdAsync(leadInfo.LeadId);
            var accountModel = await _accountService.GetAccountWithTransactionsAsync(model.AccountId, leadInfo);
            var recipientAccount = await CheckAccessAndReturnAccount(model.RecipientAccountId, leadInfo);
            var commission = CalculateCommission(model.Amount, leadInfo);
            _accountValidationHelper.CheckBalance(accountModel, model.Amount);
            model.Date = _accountValidationHelper.GetTransactionsLastDateAndThrowIfNotFound(accountModel);

            if (accountModel.Currency != Currency.RUB &&
                accountModel.Currency != Currency.USD && 
                !leadInfo.IsVip())
            {
                commission *= _commissionModifier;
                if (accountModel.Balance != model.Amount)
                {
                    throw new ValidationException(nameof(model.Amount), $"{ServiceMessages.IncompleteTransfer}");
                }
            }

            model.Amount -= commission;
            model.Currency = accountModel.Currency;
            model.RecipientCurrency = recipientAccount.Currency;
            var request = _requestHelper.CreatePostRequest(AddTransferEndpoint, model);
            var result = await _client.ExecuteAsync<List<long>>(request);

            if (result.Data == null)
            {
                throw new Exception("tstore slomalsy");
            }
            var transactionId = result.Data.First();
            EmailSender(leadDto, EmailMessages.TransferSubject, string.Format(EmailMessages.TransferBody, model.Amount, model.Currency, model.RecipientCurrency));

            var dto = new CommissionFeeDto
            {
                LeadId = leadInfo.LeadId, 
                AccountId = model.AccountId, 
                TransactionId = transactionId,
                Role = leadInfo.Role, 
                CommissionAmount = commission, 
                TransactionType = TransactionType.Transfer
            };

            dto.Id=await AddCommissionFee(dto);

            return dto;
        }

        private async Task<AccountDto> CheckAccessAndReturnAccount(int accountId, LeadIdentityInfo leadInfo)
        {
            var account = await _accountValidationHelper.GetAccountByIdAndThrowIfNotFoundAsync(accountId);
            _accountValidationHelper.CheckLeadAccessToAccount(account.LeadId, leadInfo.LeadId);
            return account;
        }

        private async Task<int> AddCommissionFee(CommissionFeeDto dto)
        {
            return await _commissionFeeService.AddCommissionFeeAsync(dto);
        }

        private decimal CalculateCommission(decimal amount, LeadIdentityInfo leadInfo)
        {
            return leadInfo.IsVip() ? amount * _vipCommission : amount * _commission;
        }

        private void EmailSender(LeadDto dto, string subject, string body)
        {
            _publishEndpoint.Publish<IMailExchangeModel>(new
            {
                Subject = subject,
                Body = $"{dto.LastName} {dto.FirstName} {body}",
                DisplayName = "Best CRM",
                MailAddresses = $"{dto.Email}"
            });
        }
    }
}