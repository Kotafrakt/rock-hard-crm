﻿using System;
using System.Collections.Generic;
using CRM.Business.Models;
using CRM.DAL.Enums;

namespace CRM.Business.Tests.TestsDataHelpers
{
    public static class TransactionBusinessModelData
    {
        public static List<TransactionBusinessModel> GetListTransactionBusinessModel()
        {
            return new()
            {
                new()
                {
                    Id = 1,
                    AccountId = 1,
                    Currency = Currency.RUB,
                    TransactionType = TransactionType.Deposit,
                    Date = DateTime.Now,
                    Amount = decimal.One
                },
                new()
                {
                    Id = 2,
                    AccountId = 2,
                    Currency = Currency.USD,
                    TransactionType = TransactionType.Deposit,
                    Date = DateTime.Now,
                    Amount = decimal.One
                }
            };
        }
    }
}