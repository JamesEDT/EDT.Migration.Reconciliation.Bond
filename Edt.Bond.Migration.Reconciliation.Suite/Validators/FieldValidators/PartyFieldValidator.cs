using Edt.Bond.Migration.Reconciliation.Framework.Extensions;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using System;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.Validators.FieldValidators
{
    class PartyFieldValidator
    {
        public static ValidationResult Validate(Document idxDocument, string expectedString, string actual)
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
    }
}
