using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleGenerator.Utilities
{
    public static class StringExt
    {
        public static void StripSpecialChars(this string str)
        {
            var charsToRemove = new string[] { "@", ",", ".", ";", "'", "\"" };
            foreach (var c in charsToRemove)
            {
                str = str.Replace(c, string.Empty);
            }
        }
    }
}
