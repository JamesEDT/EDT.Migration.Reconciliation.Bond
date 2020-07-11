using AventStack.ExtentReports;
using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Reporting;
using Edt.Bond.Migration.Reconciliation.Framework.Output;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AventStack.ExtentReports.MarkupUtils;
using HtmlTags;

namespace Edt.Bond.Migration.Reconciliation.Suite
{
    public class ComparisonTest : TestBase
    {
        public ConcurrentBag<ComparisonError> ComparisonErrors;
        public ConcurrentBag<ComparisonResult> ComparisonResults;
        public string EdtFieldUnderTest;

        [SetUp]
        public void Setup()
        {
            ComparisonErrors = new ConcurrentBag<ComparisonError>();
            ComparisonResults = new ConcurrentBag<ComparisonResult>();
        }


        [TearDown]
        public void PrintDifferencesAndResults()
        {
            if (!ComparisonErrors.Any() && !ComparisonResults.Any()) return;

            if (string.IsNullOrWhiteSpace(EdtFieldUnderTest))
            {
                Test.Log(Status.Error, "Failed to output comparison tables as Mapping under test is null");
                return;
            }

            var diffFile = PrintComparisonTables(EdtFieldUnderTest);

            Test.Info($"Difference and error details written to: <a href=\"{diffFile}\">{diffFile}</a>");

            PrintExpectedOutputFile(EdtFieldUnderTest);
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
                sw.WriteLine("DocumentId,Expected Edt value");

                foreach (var result in ComparisonResults)
                {
                    sw.WriteLine($"{result.DocumentId},\"{result.IdxConvertedValue}\"");
                }
            }
        }

        public string PrintComparisonTables(string mappingName)
        {
            //var filename = Path.Combine(Settings.ReportingDirectory,$"differences_{mappingName.Replace(" ", string.Empty)}.csv");

            //using (var sw = new StreamWriter(filename))
            //{
            //    //print comparision
            //    if (ComparisonResults.Count > 0)
            //    {
            //        sw.WriteLine("Differences:");
            //        sw.WriteLine("DocumentId,Idx value,Expected Edt value,Edt value");

            //        foreach (var result in ComparisonResults) {
            //            sw.WriteLine($"{result.DocumentId},\"{result.IdxValue}\",\"{result.IdxConvertedValue}\",\"{result.EdtValue}\"");                        
            //        }
            //    }

            //    //print errors
            //    if (ComparisonErrors.Count > 0)
            //    {
            //        sw.WriteLine();
            //        sw.WriteLine("Errors/Warnings:");
            //        sw.WriteLine("DocumentId,Error/Warning");
            //        var data = new List<string[]>() { new string[] { "<b>Errors:</b>" }, new string[] { "DocumentId", "Error message" } };

            //        foreach(var error in ComparisonErrors)
            //        {
            //            sw.WriteLine($"{error.DocumentId},{error.ErrorMessage}");
            //        }
            //    }
            //}

            return HtmlDifferencesReport.WriteReport(Settings.ReportingDirectory, mappingName.Replace(" ", string.Empty),
                ComparisonResults, ComparisonErrors);
        }

        public void LogComparisonStatistics(string[][] statistics, string tableName = null)
        {
            Test.Log(Status.Info, string.IsNullOrWhiteSpace(tableName) ? new StatsTable(statistics) : new StatsTable(statistics, tableName));
        }
    }

    public class StatsTable : IMarkup
    {
        private HtmlTag _statsTable;
        private readonly string _tableName = "Comparison Statistics";

        public StatsTable(IEnumerable<string[]> statistics, string tableName)
        {
            PopulateTable(statistics);
            _tableName = tableName;

        }
        public StatsTable(IEnumerable<string[]> statistics)
        {
            PopulateTable(statistics);
        }

        private void PopulateTable(IEnumerable<string[]> statistics)
        {
            _statsTable = new HtmlTag("table").AddClass("runtime-table")
                .Append(new HtmlTag("tr").AddClass("table-header")
                    .Append(new HtmlTag("th").Text("Statistic"))
                    .Append(new HtmlTag("th").Text("Count")));

            foreach (var stat in statistics)
            {
                _statsTable
                    .Append(new HtmlTag("tr")
                        .Append(new HtmlTag("td").Text(stat[0]))
                        .Append(new HtmlTag("td").Text(stat[1])));
            }
        }

        public string GetMarkup()
        {
            var fullHtmlString = $"<span><b>{_tableName}:</b></span><br/>{_statsTable.ToString()}";
            return fullHtmlString;
        }
    }
}
