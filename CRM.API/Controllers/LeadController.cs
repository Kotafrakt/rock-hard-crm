﻿using AutoMapper;
using CRM.API.Models;
using CRM.Business.Services;
using CRM.DAL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeadController : Controller
    {
        private readonly IMapper _mapper;
        private readonly ILeadService _leadService;

        public LeadController(IMapper mapper, ILeadService leadService)
        {
            _mapper = mapper;
            _leadService = leadService;
        }

        // api/lead
        [HttpPost]
        [Description("Add new lead")]
        [ProducesResponseType(typeof(LeadOutputModel), StatusCodes.Status201Created)]
        public ActionResult<LeadOutputModel> AddLead([FromBody] LeadInputModel model)
        {
            var dto = _mapper.Map<LeadDto>(model);
            var addedLead = _mapper.Map<LeadOutputModel>(_leadService.AddLead(dto));
            return StatusCode(201, addedLead);
        }

        // api/lead/id
        [HttpPut("{id}")]
        [Description("Update lead")]
        [ProducesResponseType(typeof(LeadOutputModel), StatusCodes.Status200OK)]
        public LeadOutputModel UpdateUserById(int id, [FromBody] LeadUpdateInputModel model)
        {
            var dto = _mapper.Map<LeadDto>(model);
            dto = _leadService.UpdateLead(id, dto);
            return _mapper.Map<LeadOutputModel>(dto);
        }

        // api/lead
        [HttpGet]
        [Description("Get all Leads")]
        [ProducesResponseType(typeof(List<LeadOutputModel>), StatusCodes.Status200OK)]
        public List<LeadOutputModel> GetAllLeads()
        {
            var listDto = _leadService.GetAllLeads();
            var listOutPut = _mapper.Map<List<LeadOutputModel>>(listDto);
            return listOutPut;
        }

        // api/lead/3
        [HttpGet("{id}")]
        [Description("Return lead by id")]
        [ProducesResponseType(typeof(LeadOutputModel), StatusCodes.Status200OK)]
        public LeadOutputModel GetLeadById(int leadId)
        {
            var dto = _leadService.GetLeadById(leadId);
            var outPut = _mapper.Map<LeadOutputModel>(dto);
            return outPut;
        }

        // api/lead/get-lead-by-email
        [HttpGet("get-lead-by-email")]
        [Description("Return lead by email")]
        [ProducesResponseType(typeof(LeadOutputModel), StatusCodes.Status200OK)]
        public LeadOutputModel GetLeadByEmail(string email)
        {
            var outPut = _leadService.GetLeadByEmail(email);
            return _mapper.Map<LeadOutputModel>(outPut);
        }

        // api/lead/3
        [HttpDelete("{id}")]
        [Description("Delete lead by id")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult DeleteLeadById(int id)
        {
            _leadService.DeleteLeadById(id);
            return NoContent();
        }

        // api/lead/account/create
        [HttpPost("account/create")]
        [Description("Create lead account")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        public ActionResult<int> AddAccount([FromBody] AccountInputModel inputModel)
        {
            var dto = _mapper.Map<AccountDto>(inputModel);
            var accountId = _leadService.AddAccount(dto);
            return StatusCode(201, accountId);
        }

        // api/lead/account/3
        [HttpDelete("account/{id}")]
        [Description("Delete lead account by id")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult DeleteAccountById(int id)
        {
            _leadService.DeleteAccount(id);
            return NoContent();
        }
        
        // api/lead/city/'name'
        [HttpGet("city/{cityName}")]
        [Description("Return leads by name of city")]
        [ProducesResponseType(typeof(List<LeadInfoOutputModel>), StatusCodes.Status200OK)]
        public List<LeadInfoOutputModel> GetLeadsByCity(string cityName)
        {
            var listLeadDtos = _leadService.GetLeadsByCity(cityName);
            var output = _mapper.Map<List<LeadInfoOutputModel>>(listLeadDtos);
            return output;
        }
    }
}