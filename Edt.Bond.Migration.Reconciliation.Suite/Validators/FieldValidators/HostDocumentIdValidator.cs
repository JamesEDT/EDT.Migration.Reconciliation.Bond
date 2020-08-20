using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using System;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.Validators.FieldValidators
{
    class HostDocumentIdValidator
    {

        public static ValidationResult Validate(Document idxDocument)
        {
            var fileType = idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals("FILETYPE_PARAMETRIC", StringComparison.InvariantCultureIgnoreCase));


            if (fileType.Value.Equals(".pst", StringComparison.InvariantCultureIgnoreCase))
            {
                return new ValidationResult
                {
                    Matched = true,
                    IsError = true,
                    ErrorMessage = "Host document id null due to being .pst"
                };               
            }
            else
            {
                return new ValidationResult
                {
                    Matched = false
                };
            }
        }
    }
}
