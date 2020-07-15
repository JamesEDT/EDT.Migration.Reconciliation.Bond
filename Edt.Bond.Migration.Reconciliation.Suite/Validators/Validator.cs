using AventStack.ExtentReports;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;

namespace Edt.Bond.Migration.Reconciliation.Suite.Validators
{
    public abstract class Validator
    {
        public ComparisonTestResult TestResult;
        private readonly string _edtName;
        private readonly string _idxFields;

        public Validator(string edtName, string sourceFields)
        {
            TestResult = new ComparisonTestResult(new StandardMapping(edtName, sourceFields, "Text", "Standard"));
            _edtName = edtName;
            _idxFields = sourceFields;
        }

        public void PrintStats(ExtentTest transformedTestsReporter)
        {
            TestResult.PrintDifferencesAndResults(transformedTestsReporter);
        }
    }
}
