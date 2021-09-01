﻿using System;
using System.Collections.Generic;
using CRM.DAL.Enums;
using CRM.DAL.Models;

namespace CRM.Business.Tests.TestsDataHelpers
{
    public static class AccountData
    {
        public static AccountDto GetAccountDto()
        {
            return new()
            {
                Id = 1,
                LeadId = 1,
                Currency = Currency.RUB,
                CreatedOn = DateTime.Now.AddYears(-1),
                IsDeleted = false
            };
        }

        public static AccountDto GetAnotherAccountDto()
        {
            return new()
            {
                Id = 2,
                LeadId = 2,
                Currency = Currency.RUB,
                CreatedOn = DateTime.Now.AddYears(-1),
                IsDeleted = false
            };
        }

        public static List<AccountDto> GetListAccount()
        {
            return new()
            {
                new()
                {
                    Id = 1,
                    LeadId = 1,
                    Currency = Currency.RUB,
                    CreatedOn = DateTime.Now.AddYears(-1),
                    IsDeleted = false
                },
                new()
                {
                    Id = 2,
                    LeadId = 1,
                    Currency = Currency.USD,
                    CreatedOn = DateTime.Now.AddYears(-1),
                    IsDeleted = false
                }
            };
        }
    }
}