﻿using CRM.DAL.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM.DAL.Models
{
    public class AccountDto
    {
        public int Id { get; set; }
        public int LeadId { get; set; }
        public Currency Currency { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? Closed { get; set; }
        public bool IsDeleted { get; set; }
    }
}
