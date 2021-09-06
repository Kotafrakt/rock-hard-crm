﻿using CRM.DAL.Enums;
using CRM.DAL.Models;
using CRM.DAL.Repositories;
using DevEdu.Business.ValidationHelpers;
using System.Collections.Generic;
using CRM.Business.IdentityInfo;

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
            var leadId = _leadRepository.AddLead(dto);
            _accountRepository.AddAccount(new AccountDto { LeadId = leadId, Currency = Currency.RUB });
            return _leadRepository.GetLeadById(leadId);
        }

        public LeadDto UpdateLead(int leadId, LeadDto dto)
        {
            _leadValidationHelper.GetLeadByIdAndThrowIfNotFound(leadId);
            dto.Id = leadId;
            _leadRepository.UpdateLead(dto);
            return _leadRepository.GetLeadById(leadId);
        }

        public List<LeadDto> GetAllLeads()
        {
            return _leadRepository.GetAllLeads();
        }

        public LeadDto GetLeadById(int leadId, LeadIdentityInfo leadInfo)
        {
            var dto= _leadValidationHelper.GetLeadByIdAndThrowIfNotFound(leadId);
            _leadValidationHelper.CheckAccessToLead(leadId,leadInfo);
            return dto;
        }

        public void DeleteLeadById(int leadId)
        {
            _leadValidationHelper.GetLeadByIdAndThrowIfNotFound(leadId);
            _leadRepository.DeleteLeadById(leadId);
        }
    }
}