using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;

namespace Edt.Bond.Migration.Reconciliation.Suite.Validators.FieldValidators
{
    class PageCountValidator
    {
        public static ValidationResult Validate(Document idxDocument, string expectedString, string actual)
        {
            if (int.TryParse(expectedString, out var expecedInt))
            {
                expectedString = expecedInt.ToString();
            }
            else
            {
                expectedString = string.Empty;
            }

            return new ValidationResult()
            {
                Matched = expectedString.Equals(actual),
                EdtComparisonValue = actual,
                ExpectedComparisonValue = expectedString
            };
        }
    }
}
