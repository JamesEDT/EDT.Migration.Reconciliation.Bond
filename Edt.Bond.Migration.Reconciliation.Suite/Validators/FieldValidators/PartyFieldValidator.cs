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

            //get party record
            var emailActual = string.Join(";", actual.Split(new char[] { ';', ',' }).Select(x => x.Trim()).Distinct().OrderBy(x => x.ToLower())).Truncate(255);
            var emailExpected = string.Join(";", expectedString.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(x => x.ToLower())).Truncate(255);

            return new ValidationResult()
            {
                Matched = emailActual.Equals(emailExpected, StringComparison.InvariantCultureIgnoreCase),
                EdtComparisonValue = emailActual,
                ExpectedComparisonValue = emailExpected
            };
        }
    }
}
