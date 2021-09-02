﻿using CRM.DAL.Enums;
using static CRM.API.Common.ValidationMessage;
using System.Collections.Generic;
using CRM.API.Common;

namespace CRM.API.Models
{
    public class LeadFiltersInputModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Patronymic { get; set; }
        public int? Role { get; set; }
        public List<int> City { get; set; }

        [CustomDateFormat(ErrorMessage = WrongDateFormat)]
        public string BirthDateFrom { get; set; }

        [CustomDateFormat(ErrorMessage = WrongDateFormat)]
        public string BirthDateTo { get; set; }
    }
}
