using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.Validators.FieldValidators
{
    class MultiValueListFieldValidator
    {
        public static ValidationResult Validate(Document idxDocument, string[] expectedValues, string actual)
        {
            var edtValues = actual.Split(new char[] { ';' });
            var expectedListValues = expectedValues.SelectMany(x => x.Replace("(", string.Empty).Replace(")", string.Empty).Split(";".ToCharArray())).ToList();
                       
            return new ValidationResult()
            {
                Matched = !(edtValues.Except(expectedListValues).Any() || expectedListValues.Except(edtValues).Any()),
                EdtComparisonValue = string.Join(";", edtValues),
                ExpectedComparisonValue = string.Join(";", expectedListValues)
            };
        }
    }
}
