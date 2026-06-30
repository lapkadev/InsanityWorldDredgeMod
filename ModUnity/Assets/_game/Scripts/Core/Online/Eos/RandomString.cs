using System;
using System.Text;

namespace InsanityWorldMod.Core
{
    public static class RandomString
    {
        public static string Generate(int size)
        {
            var builder = new StringBuilder(size);
            Random random = new Random();

            char offsetLowerCase = 'a';
            char offsetUpperCase = 'A';
            const int lettersOffset = 26;

            for (var i = 0; i < size; i++)
            {
                char offset = random.Next(0, 2) == 0 ? offsetLowerCase : offsetUpperCase;
                var ch = (char)random.Next(offset, offset + lettersOffset);
                builder.Append(ch);
            }

            return builder.ToString();
        }
    }
}
