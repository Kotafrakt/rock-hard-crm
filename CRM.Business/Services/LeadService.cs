﻿using CRM.DAL.Enums;
using CRM.DAL.Models;
using CRM.DAL.Repositories;
using DevEdu.Business.ValidationHelpers;
using System.Collections.Generic;

namespace CRM.Business.Services
{
    public class LeadService : ILeadService
    {
        private readonly ILeadRepository _leadRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILeadValidationHelper _leadValidationHelper;

        public LeadService
        (
            ILeadRepository leadRepository,
            IAccountRepository accountRepository,
            IAuthenticationService authenticationService,
            ILeadValidationHelper leadValidationHelper
        )
        {
            _leadRepository = leadRepository;
            _accountRepository = accountRepository;
            _authenticationService = authenticationService;
            _leadValidationHelper = leadValidationHelper;
        }

        public LeadDto AddLead(LeadDto dto)
        {
            dto.Password = _authenticationService.HashPassword(dto.Password);
            dto.Role = Role.Regular;
            dto.BirthYear = dto.BirthDate.Year;
            dto.BirthMonth = dto.BirthDate.Month;
            dto.BirthDay = dto.BirthDate.Day;
            var id = _leadRepository.AddLead(dto);

            _accountRepository.AddAccount(new AccountDto { LeadId = id, Currency = Currency.RUB });
            return _leadRepository.GetLeadById(id);
        }

        public LeadDto UpdateLead(int id, LeadDto dto)
        {
            _leadValidationHelper.GetLeadByIdAndThrowIfNotFound(id);
            dto.Id = id;
            _leadRepository.UpdateLead(dto);
            return _leadRepository.GetLeadById(id);
        }

        public List<LeadDto> GetAllLeads()
        {
            return _leadRepository.GetAllLeads();
        }

        public LeadDto GetLeadById(int id)
        {
            return _leadValidationHelper.GetLeadByIdAndThrowIfNotFound(id);
        }

        public void DeleteLeadById(int id)
        {
            _leadValidationHelper.GetLeadByIdAndThrowIfNotFound(id);
            _leadRepository.DeleteLeadById(id);
        }
    }
}