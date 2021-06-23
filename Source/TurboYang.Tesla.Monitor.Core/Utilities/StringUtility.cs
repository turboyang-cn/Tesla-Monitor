using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurboYang.Tesla.Monitor.Core.Utilities
{
    public class StringUtility
    {
        public static String RandomString(Int32 length)
        {
            const String pool = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            StringBuilder resultBuilder = new();

            resultBuilder.Append(new String(pool.OrderBy(x => Guid.NewGuid()).Take(Math.Min(length, pool.Length)).ToArray()));

            if (length > pool.Length)
            {
                resultBuilder.Append(RandomString(length - pool.Length));
            }

            return resultBuilder.ToString();
        }

        public static String ObfuscateString(String content)
        {
            if (content == null)
            {
                return null;
            }

            Char[] contentArray = content.ToArray();
            Int32 obfuscateIndex = (Int32)Math.Ceiling(content.Length / 3.0);
            Int32 obfuscateLength = (Int32)Math.Floor(content.Length / 3.0);

            for (Int32 i = 0; i < obfuscateLength; i++)
            {
                contentArray[obfuscateIndex + i] = '*';
            }

            return new String(contentArray);
        }
    }
}
