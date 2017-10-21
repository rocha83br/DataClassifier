using System;
using System.Linq;
using System.Text;

namespace Rochas.DataClassifier.Extensions
{
    public static class StringExtension
    {
        public static ulong GetCustomHashCode(this string value)
        {
            if (value.Length > 0)
            {
                var preResult = string.Empty;

                var charArray = Encoding.ASCII.GetBytes(value);
                var byteSum = charArray.Sum(chr => Math.Round(Math.Log(chr), 6));

                preResult = byteSum.ToString().Replace(".", string.Empty).Replace(",", string.Empty);

                return ulong.Parse(preResult);
            }
            else
                return 0;
        }

        public static string ToTitleCase(this string value)
        {
            return string.Concat(value.First().ToString().ToUpper(), value.Substring(1).ToLower());
        }
    }
}
