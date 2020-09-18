using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.Validators.FieldValidators
{
    class CustomEmailFieldValidator
    {
        //compares distinct list
        public static ValidationResult Validate(Document idxDocument, string[] expectedValues, string actual)
        {
            var actualList = actual.Split(";".ToCharArray()).Select(x => x.Trim()).Distinct();
            var expectedDistinct = expectedValues.Select(x => x.Trim()).Distinct();

            return new ValidationResult()
            {
                Matched = !(actualList.Except(expectedDistinct).Any() || expectedDistinct.Except(actualList).Any()) || actual.Length >= 350,
                EdtComparisonValue = string.Join(";", expectedDistinct),
                ExpectedComparisonValue = string.Join(";", actualList)
            };
        }
    }
}
