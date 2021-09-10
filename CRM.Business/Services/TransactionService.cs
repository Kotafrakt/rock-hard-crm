﻿using CRM.Business.IdentityInfo;
using CRM.Business.Models;
using CRM.Business.Requests;
using CRM.Core;
using CRM.DAL.Enums;
using CRM.DAL.Models;
using Microsoft.Extensions.Options;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using CRM.Business.ValidationHelpers;
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

        public TransactionService
        (
            IOptions<ConnectionSettings> connectionOptions,
            IOptions<CommissionSettings> commissionOptions,
            IAccountValidationHelper accountValidationHelper,
            IAccountService accountService, 
            ICommissionFeeService commissionFeeService
        )
        {
            _client = new RestClient(connectionOptions.Value.TransactionStoreUrl);
            _commission = commissionOptions.Value.Commission; 
            _vipCommission = commissionOptions.Value.VipCommission;
            _commissionModifier = commissionOptions.Value.CommissionModifier;
            _requestHelper = new RequestHelper();
            _accountValidationHelper = accountValidationHelper;
            _accountService = accountService;
            _commissionFeeService = commissionFeeService;
        }

        public TransactionBusinessModel AddDeposit(TransactionBusinessModel model, LeadIdentityInfo leadInfo)
        {
            var account = CheckAccessAndReturnAccount(model.AccountId, leadInfo);
            _accountValidationHelper.CheckForVipAccess(account.Currency, leadInfo);
            var commission = CalculateCommission(model.Amount, leadInfo);

            model.Amount -= commission;
            model.AccountId = account.Id;
            model.Currency = account.Currency;

            var request = _requestHelper.CreatePostRequest(AddDepositEndpoint, model);
            var result = _client.Execute<long>(request);
            var transactionId = result.Data;

            var transaction = new TransactionBusinessModel
            {
                AccountId = account.Id,
                Amount = model.Amount,
                CommissionFee = commission,
                Currency = account.Currency,
                Id = transactionId,
                TransactionType = TransactionType.Withdraw,
                Date = null
            };

            AddCommissionFee(leadInfo,transactionId,model, commission);

            return transaction;
        }

        public TransactionBusinessModel AddWithdraw(TransactionBusinessModel model, LeadIdentityInfo leadInfo)
        {
            var account = CheckAccessAndReturnAccount(model.AccountId, leadInfo);
            _accountValidationHelper.CheckForVipAccess(account.Currency, leadInfo);
            var commission= CalculateCommission(model.Amount, leadInfo);

            model.Amount -= commission;
            model.AccountId = account.Id;
            model.Currency = account.Currency;

            var request = _requestHelper.CreatePostRequest(AddWithdrawEndpoint, model);
            var result = _client.Execute<long>(request);
            var transactionId = result.Data;

            var transaction = new TransactionBusinessModel
            {
                AccountId = account.Id,
                Amount = model.Amount,
                CommissionFee = commission,
                Currency = account.Currency,
                Id = transactionId,
                TransactionType = TransactionType.Withdraw,
                Date = null
            };

            AddCommissionFee(leadInfo, transactionId, model, commission);

            return transaction;
        }

        public TransferBusinessModel AddTransfer(TransferBusinessModel model, LeadIdentityInfo leadInfo)
        {
            var account = CheckAccessAndReturnAccount(model.AccountId, leadInfo);
            var recipientAccount = CheckAccessAndReturnAccount(model.RecipientAccountId, leadInfo);
            var commission = CalculateCommission(model.Amount, leadInfo);
            if (account.Currency != Currency.RUB && account.Currency != Currency.USD && !leadInfo.IsVip())
            {
                commission *= _commissionModifier;
                //var balance = _accountService.GetAccountWithTransactions(account.Id, leadInfo).Balance;
                //if (balance != model.Amount)
                //{
                //    throw new Exception("снять можно только все бабки простак");
                //}
            }
            
            model.Amount -= commission;
            model.Currency = account.Currency;
            model.RecipientCurrency = recipientAccount.Currency;
            var request = _requestHelper.CreatePostRequest(AddTransferEndpoint, model);
            var result = _client.Execute<List<long>>(request);

            var transactionId = result.Data.First();
            var recipientTransactionId = result.Data.Last();

            AddCommissionFee(leadInfo, transactionId, model, commission);

            var transaction = new TransferBusinessModel
            {
                AccountId = account.Id,
                Amount = model.Amount,
                CommissionFee = commission,
                Currency = account.Currency,
                Id = transactionId,
                TransactionType = TransactionType.Transfer,
                RecipientTransactionId=recipientTransactionId,
                RecipientAccountId = model.RecipientAccountId,
                Date = null,
            };

            return transaction;
        }

        private AccountDto CheckAccessAndReturnAccount(int accountId, LeadIdentityInfo leadInfo)
        {
            var account = _accountValidationHelper.GetAccountByIdAndThrowIfNotFound(accountId);
            _accountValidationHelper.CheckLeadAccessToAccount(account.LeadId, leadInfo.LeadId);
            return account;
        }

        private void AddCommissionFee(LeadIdentityInfo leadInfo, long transactionId, TransactionBusinessModel model, decimal commission)
        {
            var role = leadInfo.IsVip() ? Role.Vip : Role.Regular;
            //var role = leadInfo.Roles.First();
            var dto = new CommissionFeeDto
                {LeadId = leadInfo.LeadId, AccountId = model.AccountId, TransactionId = transactionId, Role = role, Amount = commission };
            _commissionFeeService.AddCommissionFee(dto);
        }

        private decimal CalculateCommission(decimal amount, LeadIdentityInfo leadInfo)
        {
            return leadInfo.IsVip() ? amount * _vipCommission : amount * _commission;
        }
    }
}