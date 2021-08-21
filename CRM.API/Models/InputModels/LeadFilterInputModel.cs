﻿using CRM.DAL.Enums;
using System.Collections.Generic;

namespace CRM.API.Models
{
    public class LeadFilterInputModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Patronymic { get; set; }
        public List<Role> Roles { get; set; }
        public List<CityInputModel> Cities { get; set; }
        public string BirthDateFrom { get; set; }
        public string BirthDateTo { get; set; }
    }
}
