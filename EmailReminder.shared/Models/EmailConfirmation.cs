﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EmailReminder.shared.Models
{
    public class EmailConfirmation
    {
        public string Email { get; set; }
        public string Token { get; set; }
    }
}
