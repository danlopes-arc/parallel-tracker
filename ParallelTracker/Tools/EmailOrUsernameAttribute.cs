using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParallelTracker.Tools
{
    public class EmailOrUsernameAttribute : ValidationAttribute
    {
        public EmailOrUsernameAttribute()
        {
            ErrorMessage = "The {0} may only have letters, digits or the following symbols -_";
        }
        public override bool IsValid(object? value)
        {
            if (value == null)
            {
                return true;
            }

            if (!(value is string valueAsString))
            {
                return false;
            }

            return IsEmail(valueAsString) ||
                   Regex.IsMatch(valueAsString,
                       @"^[abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789\-_]+$");
        }
        
        private bool IsEmail(string value)
        {
            if (value == null)
            {
                return true;
            }

            // only return true if there is only 1 '@' character
            // and it is neither the first nor the last character
            int index = value.IndexOf('@');

            return
                index > 0 &&
                index != value.Length - 1 &&
                index == value.LastIndexOf('@');
        }
    }
}
