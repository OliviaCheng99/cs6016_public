// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using LMS.Models.LMSModels;

namespace LMS.Areas.Identity.Pages.Account
{
    internal class Professors : Professor
    {
        public string UId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime Dob { get; set; }
        public string Department { get; set; }
    }
}