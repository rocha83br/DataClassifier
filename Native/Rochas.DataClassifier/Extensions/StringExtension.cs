using System;
using System.Linq;
using System.Text;

namespace Rochas.DataClassifier.Extensions
{
    public static class StringExtension
    {
        public static uint GetCustomHashCode(this string value)
        {
            if (value.Length > 0)
            {
                var preResult = string.Empty;

                var firstChar = Encoding.ASCII.GetBytes(value.First().ToString());
                var lastChar = Encoding.ASCII.GetBytes(value.Last().ToString());
                var charArray = Encoding.ASCII.GetBytes(value);
                var byteSum = charArray.Sum(chr => chr) + ((value.Length + 1) / 2);

                preResult = string.Concat(firstChar.First(), byteSum, lastChar.First());

                return uint.Parse(preResult);
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
