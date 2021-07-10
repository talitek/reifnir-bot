using System;
using System.Collections.Generic;
using System.Text;

namespace Nellebot.Common.Extensions
{
    public static class StringExtensions
    {
        public static string RemoveQuotes(this string input)
        {
            return string.IsNullOrEmpty(input) ? string.Empty : input.Trim('"');
        }
    }
}
