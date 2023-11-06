using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace System
{
    public static class StringExtensionForParadoxFiles
    {
        /// <summary>
        /// REMOVE THE COMMENTS BEFORE CLEANING THE CARIAGE RETURNS!
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string RemoveComment(this string input) => Regex.Replace(input.ReplaceLineEndings(), @"\#.*(?=\r\n)", "", RegexOptions.Multiline);
        public static string RemoveMultipleWhiteSpacesAndReturns(this string input) => Regex.Replace(input.ReplaceLineEndings(), @$"\s\s+", " ");
        public static string GetNextValueBetweenBrackets(this string input, out int last)
        {
            if(input.Length < 2)
            {
                last = -1;
                return "";
            }
            var result = input.BraceMatch(out var first, out last);
            
            if(input.Length <= first){
                last = -1;
                return "";
            };
            return result[(first+1)..last].Trim();
        }

        public static string BraceMatch(this string input, out int firstInstance, out int lastInstance)
        {
            int level = 0;
            int currentIndex = -1;
            firstInstance = input.Length;
            lastInstance = -1;
            foreach (var c in input)
            {
                if (level < 0) return input;

                currentIndex++;
                if (c == '{')
                {
                    level++;
                    firstInstance = currentIndex < firstInstance? currentIndex : firstInstance;
                }
                else if (c == '}')
                {
                    level--;
                    if(level == 0)
                    {
                        lastInstance = currentIndex > lastInstance? currentIndex : lastInstance;
                        return input;
                    }
                }
            }

            if (level > 0) throw new ApplicationException("Closing brace missing.");
            return input;
        }
    }
}