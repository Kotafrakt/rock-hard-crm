﻿using System;
using System.Text;
using CRM.Business.IdentityInfo;
using CRM.Business.Models;
using CRM.Business.Requests;
using CRM.Core;
using CRM.DAL.Enums;
using CRM.DAL.Models;
using DevEdu.Business.ValidationHelpers;
using Microsoft.Extensions.Options;
using RestSharp;
using static CRM.Business.TransactionEndpoint;

namespace CRM.Business.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly RestClient _client;
        private readonly RequestHelper _requestHelper;
        private readonly IAccountValidationHelper _accountValidationHelper;
        private readonly IAccountService _accountService;
        private readonly double _commission;
        private readonly double _vipCommission;
        private const double _commissionModifier = 1.5;

        public TransactionService
        (
            IOptions<ConnectionUrl> connectionOptions,
            IOptions<CommissionSettings> commissionOptions,
            IAccountValidationHelper accountValidationHelper,
            IAccountService accountService
        )
        {
            _client = new RestClient(connectionOptions.Value.TstoreUrl);
            _commission = commissionOptions.Value.Commission;
            _vipCommission = commissionOptions.Value.VipCommission;
            _requestHelper = new RequestHelper();
            _accountValidationHelper = accountValidationHelper;
            _accountService = accountService;
        }

        public long AddDeposit(TransactionBusinessModel model, LeadIdentityInfo leadInfo)
        {
            var account = CheckAccessAndReturnAccount(model.AccountId, leadInfo);
            _accountValidationHelper.CheckForVipAccess(account.Currency, leadInfo);

            model.AccountId = account.Id;
            model.Currency = account.Currency;

            var request = _requestHelper.CreatePostRequest(AddDepositEndpoint, model);
            var result = _client.Execute<long>(request);
            return result.Data;
        }

        public long AddWithdraw(TransactionBusinessModel model, LeadIdentityInfo leadInfo)
        {
            var account = CheckAccessAndReturnAccount(model.AccountId, leadInfo);
            _accountValidationHelper.CheckForVipAccess(account.Currency, leadInfo);

            model.AccountId = account.Id;
            model.Currency = account.Currency;

            var request = _requestHelper.CreatePostRequest(AddWithdrawEndpoint, model);
            var result = _client.Execute<long>(request);
            return result.Data;
        }

        public string AddTransfer(TransferBusinessModel model, LeadIdentityInfo leadInfo)
        {
            var account = CheckAccessAndReturnAccount(model.AccountId, leadInfo);
            var recipientAccount = CheckAccessAndReturnAccount(model.RecipientAccountId,leadInfo);

            if (account.Currency is not (Currency.RUB or Currency.USD) && !leadInfo.IsVip())
            {
                var balance=_accountService.GetAccountWithTransactions(account.Id, leadInfo).Balance;
                if (balance!=model.Amount)
                {
                    throw new Exception("снять можно только все бабки простак");
                }
            }

            model.Currency = account.Currency;
            model.RecipientCurrency = recipientAccount.Currency;
            var request = _requestHelper.CreatePostRequest(AddTransferEndpoint, model);
            var result = _client.Execute<string>(request);
            return result.Data;
        }

        private AccountDto CheckAccessAndReturnAccount(int accountId, LeadIdentityInfo leadInfo)
        {
            var account = _accountValidationHelper.GetAccountByIdAndThrowIfNotFound(accountId);
            _accountValidationHelper.CheckLeadAccessToAccount(account.LeadId, leadInfo.LeadId);
            //_accountValidationHelper.CheckForVipAccess(account.Currency, leadInfo);
            return account;
        }
    }
}