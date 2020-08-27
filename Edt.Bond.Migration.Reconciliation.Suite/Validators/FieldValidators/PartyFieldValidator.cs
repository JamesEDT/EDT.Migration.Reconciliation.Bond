using Edt.Bond.Migration.Reconciliation.Framework.Extensions;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.Validators.FieldValidators
{
    class PartyFieldValidator
    {
        public static ValidationResult Validate(StandardMapping mapping, Document idxDocument, string expectedString, string actual)
        {
            var actualEmails = actual.Split(new char[] { ';', ',' }).Select(x => x.Trim()).Distinct(StringComparer.CurrentCultureIgnoreCase);
            var exepctedEmails = expectedString.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Distinct(StringComparer.CurrentCultureIgnoreCase);

            var extraActual = actualEmails.Except(exepctedEmails);
            var missingExpected = exepctedEmails.Except(actualEmails);

            if (!extraActual.Any() && !missingExpected.Any())
                return new ValidationResult()
                {
                    Matched = true
                };

           // var rebuiltExpected = RebuildFromIdx(mapping, idxDocument);

           /// extraActual = actualEmails.Except(rebuiltExpected);
           // missingExpected = rebuiltExpected.Except(actualEmails);

            if (!extraActual.Any() && !missingExpected.Any())
                return new ValidationResult()
                {
                    Matched = true
                };


            //get party record
            var emailActual = string.Join(";", actual.Split(new char[] { ';', ',' }).Select(x => x.Trim().Truncate(255)).Distinct().OrderBy(x => x.ToLower()));
            var emailExpected = string.Join(";", expectedString.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim().Truncate(255)).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(x => x.ToLower()));

            return new ValidationResult()
            {
                Matched = emailActual.Equals(emailExpected, StringComparison.InvariantCultureIgnoreCase),
                EdtComparisonValue = actual,
                ExpectedComparisonValue = string.Join(";", exepctedEmails)
            };
        }

        private static List<string> RebuildFromIdx(StandardMapping mapping, Document document)
        {
            var matchedIdxFields = document.GetValuesForIdolFields(mapping.IdxNames).ToList();

            var idxValues = new List<string>();

            for (var v = 0; v < matchedIdxFields.Count; v++)
            {
                if (!matchedIdxFields[v].Contains("@")
                    && (v + 1 < matchedIdxFields.Count)
                    && matchedIdxFields[v + 1].StartsWith(" ")
                    && matchedIdxFields[v + 1].Contains("@"))
                {
                    idxValues.Add($"{matchedIdxFields[v]}, {matchedIdxFields[v + 1]}".Replace("  ", " ").Trim());
                    v++;

                }
                else
                {
                    idxValues.Add(matchedIdxFields[v]);
                }
            }

            return idxValues;
        }
    }
}
