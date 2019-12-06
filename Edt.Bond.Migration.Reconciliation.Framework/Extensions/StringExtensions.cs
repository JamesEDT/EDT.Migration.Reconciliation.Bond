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
    }
}
