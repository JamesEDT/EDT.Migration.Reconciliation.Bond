using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using System;

namespace Edt.Bond.Migration.Reconciliation.Suite.Validators.FieldValidators
{
    class FileExtensionValidator
    {
        public static NativeFileFinder _nativeFileFinder;
        public static ValidationResult Validate(Document idxDocument, string expected, string edtFileExtension)
        {
           
            if (_nativeFileFinder == null)
                _nativeFileFinder = new NativeFileFinder();

            var actualFileExtension = _nativeFileFinder.GetExtension(idxDocument.DocumentId) ?? ".txt";
   
            return new ValidationResult()
            {
                Matched = actualFileExtension != null && actualFileExtension.Equals(edtFileExtension, StringComparison.InvariantCultureIgnoreCase),
                EdtComparisonValue = edtFileExtension,
                ExpectedComparisonValue = actualFileExtension
            };
        }
    }
}
