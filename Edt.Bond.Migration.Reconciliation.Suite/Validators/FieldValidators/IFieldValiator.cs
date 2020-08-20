namespace Edt.Bond.Migration.Reconciliation.Suite.Validators.FieldValidators
{
    interface IFieldValidator
    {
        ValidationResult Validate(string IdxValue, string EdtValue, out string expectedValue);
    }
}
