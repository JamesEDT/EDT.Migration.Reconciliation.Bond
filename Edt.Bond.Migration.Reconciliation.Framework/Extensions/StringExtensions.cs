using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Edt.Bond.Migration.Reconciliation.Framework.Extensions
{
    public static class StringExtensions
    {
        public static readonly Regex CsvSplit = new Regex("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)", RegexOptions.Compiled);

        public static string[] SplitCsv(this string input)
        {

            List<string> list = new List<string>();
            string curr = null;
            foreach (Match match in CsvSplit.Matches(input))
            {
                curr = match.Value;
                if (0 == curr.Length)
                {
                    list.Add("");
                }

                list.Add(curr.TrimStart(','));
            }

            return list.ToArray();
        }

        public static string[] SplitCsv(this string input, bool sort)
        {
            var tokens = SplitCsv(input).ToList();

            if(sort) tokens.Sort();

            return tokens.ToArray();
        }

        public static string TrimNonAlphaNumerics(this string input)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            return rgx.Replace(input, "");
        }

        public static string ReplaceTagChars(this string input)
        {
            return input?.Replace('/', '~').Replace('.', '~').Replace('-','~');
        }


        public static List<string> LowMemSplit(this string s, string seperator)
        {
            List<string> list = new List<string>();
            int lastPos = 0;
            int pos = s.IndexOf(seperator);
            while (pos > -1)
            {
                while (pos == lastPos)
                {
                    lastPos += seperator.Length;
                    pos = s.IndexOf(seperator, lastPos);
                    if (pos == -1)
                        return list;
                }

                string tmp = s.Substring(lastPos, pos - lastPos);
                if (!string.IsNullOrWhiteSpace(tmp))
                    list.Add(tmp);
                lastPos = pos + seperator.Length;
                pos = s.IndexOf(seperator, lastPos);
            }

            if (lastPos < s.Length)
            {
                string tmp = s.Substring(lastPos, s.Length - lastPos);
                if (!string.IsNullOrWhiteSpace(tmp))
                    list.Add(tmp);
            }

            return list;
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

    }
}
