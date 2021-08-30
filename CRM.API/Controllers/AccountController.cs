﻿using AutoMapper;
using CRM.API.Models;
using CRM.Business.Models;
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
    public class AccountController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IAccountService _accountService;

        public AccountController(IMapper mapper, IAccountService accountService)
        {
            _mapper = mapper;
            _accountService = accountService;
        }

        // api/account/create
        [HttpPost("account/create")]
        [Description("Create lead account")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        public ActionResult<int> AddAccount([FromBody] AccountInputModel inputModel)
        {
            var dto = _mapper.Map<AccountDto>(inputModel);
            var accountId = _accountService.AddAccount(dto);
            return StatusCode(201, accountId);
        }

        // api/account/3
        [HttpDelete("account/{id}")]
        [Description("Delete lead account by id")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult DeleteAccountById(int id)
        {
            _accountService.DeleteAccount(id);
            return NoContent();
        }

        // api/account/transaction
        [HttpGet("by-account/{accountId}")]
        [Description("Get transactions by account")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        public ActionResult<List<TransactionBusinessModel>> AddTransaction(int accountId)
        {
            var output = _accountService.GetTransactionsByAccountId(accountId);
            return StatusCode(201, output);
        }
    }
}