using System;
using System.Text;

namespace Rochas.SoundEx
{
    public static class RochasSoundEx
    {
        #region Public Methods

        public static string Generate(string value)
        {
            StringBuilder result = new StringBuilder();

            if (value.Length > 0)
            {
                result.Append(char.ToUpper(value[0]));

                for (int count = 1; count < value.Length && result.Length < 4; count++)
                {
                    string c = applyRules(value[count]);

                    if (count == 1)
                        result.Append(c);
                    else if (c != applyRules(value[count - 1]))
                        result.Append(c);
                }

                for (int count = result.Length; count < 4; count++)
                    result.Append("0");
            }

            return result.ToString();
        }

        #endregion

        #region Helper Methods

        private static string applyRules(char c)
        {
            switch (char.ToLower(c))
            {
                case 'b':
                case 'f':
                case 'p':
                case 'v':
                    return "1";
                case 'c':
                case 'g':
                case 'j':
                case 'k':
                case 'q':
                case 's':
                case 'x':
                case 'z':
                    return "2";
                case 'd':
                case 't':
                    return "3";
                case 'l':
                    return "4";
                case 'm':
                case 'n':
                    return "5";
                case 'r':
                    return "6";
                default:
                    return string.Empty;
            }
        }

        #endregion
    }
}
