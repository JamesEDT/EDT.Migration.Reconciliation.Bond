using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.Validators.FieldValidators
{
    class DateFieldValidator
    {
        public static ValidationResult Validate(Document idxDocument, StandardMapping mapping, string[] expectedValues, string actual)
        {
            var gotActualDate = DateTime.TryParse(actual, out var actualDate);

            var gotExpectedDate = DateTime.TryParse(expectedValues?.FirstOrDefault(), out var expectedDate);


            if (mapping.EdtName.Equals("Date") && !gotExpectedDate)
            {
                gotExpectedDate = DateTime.TryParse(idxDocument.GetValuesForIdolFields(new List<string>() { "FILEMODIFIEDTIME" })?.FirstOrDefault(), out expectedDate);
            }


            return new ValidationResult()
            {
                Matched = (gotActualDate && gotExpectedDate && actualDate.Equals(expectedDate) || (!gotActualDate && !gotExpectedDate)),
                EdtComparisonValue = actualDate.ToString(),
                ExpectedComparisonValue = expectedDate.ToString()
            };
        }
    }
}
