namespace Edt.Bond.Migration.Reconciliation.Suite.Validators.FieldValidators
{
    interface IFieldValiator
    {
        bool Validate(string IdxValue, string EdtValue, out string expectedValue);
    }
}
