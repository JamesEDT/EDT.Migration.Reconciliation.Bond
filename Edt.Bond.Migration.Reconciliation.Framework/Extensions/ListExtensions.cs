using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Edt.Bond.Migration.Reconciliation.Framework.Extensions
{
    public static class ListExtensions
    {

        public static List<string> GetDifferences(List<string> input, List<string> toCompare)
        {

            var exceptInInput = input.Except(toCompare).ToList();
            var exceptInToCompare = toCompare.Except(input);

            exceptInInput.AddRange(exceptInToCompare);

            return exceptInInput;
        }

        public static void DifferencesToFile(List<string> input, List<string> toCompare, string filePath)
        {
            var except = input.Select(x => x.ToLower()).Except(toCompare.Select(x => x.ToLower())).ToList();

            WriteToFile(except, filePath);
        }

        public static void WriteToFile( List<string> input, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                input.ForEach(x => writer.WriteLine(x));
            }
        }
    }
}
