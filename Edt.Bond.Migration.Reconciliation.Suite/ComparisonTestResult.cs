using AventStack.ExtentReports;
using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Reporting;
using Edt.Bond.Migration.Reconciliation.Framework.Output;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite
{
    public class ComparisonTestResult
    {
        public ConcurrentBag<ComparisonError> ComparisonErrors;
        public ConcurrentBag<ComparisonResult> ComparisonResults;
        public StandardMapping EdtFieldUnderTest;

        public int Populated = 0;
        public int Different = 0;
        public int DocumentsInIdxButNotInEdt = 0;
        public int DocumentsInEdtButNotInIdx = 0;
        public int IdxNoValue = 0;
        public int UnexpectedErrors = 0;
        public int Matched = 0;
        public int TotalSampled = 0;

        public ComparisonTestResult(StandardMapping mapping)
        {
            ComparisonErrors = new ConcurrentBag<ComparisonError>();
            ComparisonResults = new ConcurrentBag<ComparisonResult>();
            EdtFieldUnderTest = mapping; ;
        }


        
        public void PrintDifferencesAndResults(ExtentTest testSuite)
        {
            var testLog = testSuite.CreateNode($"{EdtFieldUnderTest.EdtName} vs {string.Join("|",EdtFieldUnderTest.IdxNames)}");
            
            LogComparisonStatistics(testLog, new string[][]
            {
                new string[]{ "Matched", Matched.ToString()},
                new string[]{ "Different", Different.ToString()},
                new string[]{ "In Edt but not in IDX", DocumentsInEdtButNotInIdx.ToString()},
                new string[]{ "In Idx but not in EDT", DocumentsInIdxButNotInEdt.ToString()}
            });


            if (!ComparisonErrors.Any() && !ComparisonResults.Any()) return;

            if (string.IsNullOrWhiteSpace(EdtFieldUnderTest.EdtName))
            {
                testLog.Log(Status.Error, "Failed to output comparison tables as Mapping under test is null");
                return;
            }

            var diffFile = PrintComparisonTables(EdtFieldUnderTest.EdtName);

            testLog.Info($"Difference and error details written to: <a href=\"{diffFile}\">{diffFile}</a>");

            PrintExpectedOutputFile(EdtFieldUnderTest.EdtName);

            if(ComparisonResults.Any() || ComparisonErrors.Any())
            {
                testLog.Fail("Differences observed");
            }
            else
            {
                testLog.Pass("No differences seen");
            }
        }

        public void AddComparisonError(string documentId, string comparisonError)
        {
            ComparisonErrors.Add(new ComparisonError(documentId, comparisonError));
        }

        public void AddComparisonResult(string documentId, string edtValue, string expectedResult, string idxValue)
        {
            ComparisonResults.Add(new ComparisonResult(documentId, edtValue, expectedResult, idxValue));
        }

        public void PrintExpectedOutputFile(string mappingName)
        {
            if (ComparisonResults.Count <= 0) return;

            var filename = Path.Combine(Settings.ReportingDirectory, $"expectedvalues_{mappingName.Replace(" ", string.Empty)}.csv");

            using (var sw = new StreamWriter(filename))
            {
                sw.WriteLine($"Document ID,{mappingName}");

                foreach (var result in ComparisonResults)
                {
                    sw.WriteLine($"{result.DocumentId},\"{result.IdxConvertedValue?.Replace("\"","\"\"")}\"");
                }
            }
        }

        public string PrintComparisonTables(string mappingName)
        {
            return HtmlDifferencesReport.WriteReport(Settings.ReportingDirectory, mappingName.Replace(" ", string.Empty),
                ComparisonResults, ComparisonErrors);
        }

        public void LogComparisonStatistics(ExtentTest test, string[][] statistics, string tableName = null)
        {
            test.Log(Status.Info, string.IsNullOrWhiteSpace(tableName) ? new StatsTable(statistics) : new StatsTable(statistics, tableName));
        }
    }

    
}
