using System.Text.RegularExpressions;

namespace HoboBot.Extensions
{
    public static class VkString
    {
        public static string VkTrim(this string str)
        {
            str = str.Trim();
            while (str.StartsWith(@"\n"))
                str = new Regex(@"\\n").Replace(str, " ", 1).Trim();
            while (str.EndsWith(@"\n"))
                str = new Regex(@"\\n", RegexOptions.RightToLeft).Replace(str, " ", 1).Trim();
            return str;
        }

        public static string GetVkString(string[] stringArray, bool needUpperFirstLetter = false, int startIndex = 0)
        {
            string res = null;
            for (int i = startIndex; i < stringArray.Length; i++)
                res += stringArray[i] + " ";
            res = res.VkTrim();
            if (needUpperFirstLetter)
                res = char.ToUpper(res[0]) + res[1..];
            return res;
        }
    }
}
