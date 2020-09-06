using System;
using System.Collections.Generic;
using System.Text;

namespace Nativa
{
    class Wildcard
    {
        private string pattern;

        public Wildcard(string pattern)
        {
            this.pattern = pattern;
        }

        public bool IsMatch(string str)
        {
            int ptrStr = 0;
            int ptrPat = 0;
            int lastStarAppearance = -1;
            int matchUpTo = -1;
            while (ptrStr < str.Length)
            {
                if (ptrPat < pattern.Length && (str[ptrStr] == pattern[ptrPat] || pattern[ptrPat] == '?'))
                {
                    ptrStr++;
                    ptrPat++;
                }
                else if (ptrPat < pattern.Length && pattern[ptrPat] == '*')
                {
                    lastStarAppearance = ptrPat;
                    matchUpTo = ptrStr;
                    ptrPat = lastStarAppearance + 1;
                }
                else if (lastStarAppearance != -1)
                {
                    matchUpTo++;
                    ptrStr = matchUpTo;
                    ptrPat = lastStarAppearance + 1;
                }
                else
                {
                    return false;
                }
            }
            while (ptrPat < pattern.Length && pattern[ptrPat] == '*') ptrPat++;
            return ptrPat == pattern.Length;
        }
    }
}
