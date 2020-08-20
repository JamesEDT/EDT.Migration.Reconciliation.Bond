namespace Edt.Bond.Migration.Reconciliation.Suite.Validators.FieldValidators
{
    public class ValidationResult
    {
        public bool Matched;
        public string ExpectedComparisonValue;
        public string EdtComparisonValue;

        public bool IsError;
        public string ErrorMessage;
    }
}
