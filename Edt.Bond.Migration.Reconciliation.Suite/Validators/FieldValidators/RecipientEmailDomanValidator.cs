using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;

namespace Edt.Bond.Migration.Reconciliation.Suite.Validators.FieldValidators
{
    class RecipientEmailDomanValidator
    {

        public static ValidationResult Validate(Document idxDocument, string expectedString, string actual)
        {
           
            return new ValidationResult
            {
                Matched = expectedString.Length > 500
            };               
            
        }
    }
}
