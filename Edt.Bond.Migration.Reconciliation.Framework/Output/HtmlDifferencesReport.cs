using System;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Reporting;
using HtmlTags;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Edt.Bond.Migration.Reconciliation.Framework.Output
{
    public class HtmlDifferencesReport
    {
        private static string _css = @"<style>
span {
  font-weight: bold;
}


table {
  border-collapse: collapse;
  margin-bottom: 50px;
  margin-top: 10px;
}

th {
  background-color: #334a65;
  color: white;
  padding: 5px;
}

table, th, td {
  border: 1px solid gray;
  text-align: left;
  
}

tr:nth-child(even) {background-color: #f2f2f2;}

td{
 padding-left: 10px;
 padding-right: 10px;
 padding-top: 2px; 
 padding-bottom: 2px;
}

</style>";

        public static string WriteReport(string directory, string name, ConcurrentBag<ComparisonResult> results, ConcurrentBag<ComparisonError> errors)
        {
            var report = new HtmlTag("html")
                .Append(new HtmlTag("head")
                    .AppendHtml(_css));

            var reportContent = new HtmlTag("body");
            reportContent.Append(BuildSummary(results.Count, errors.Count));

            if(results.Any())
                reportContent.Append(BuildDifferences(results));

            if(errors.Any())
                reportContent.Append(BuildErrors(errors));

            report.Append(reportContent);

            var filePath = Path.Combine(directory, $"{name}.html");
            using (var sw = new StreamWriter(filePath))
            {
                sw.Write(report.ToHtmlString());
            }

            return $"{name}.html";
        }

        private static HtmlTag BuildSummary(int diffCount, int errorCount)
        {
            return
                new HtmlTag("table")
                .Append(new HtmlTag("tr")
                .Append(new HtmlTag("th").Text("Summary"))
                .Append(new HtmlTag("th").Text("Count")))
                .Append(new HtmlTag("tr")
                .Append(new HtmlTag("td").Append(new HtmlTag("a").Attr("href",(diffCount > 0 ? "#difference": string.Empty)).Text("Differences")))
                .Append(new HtmlTag("td").Text(diffCount.ToString())))
                .Append(new HtmlTag("tr")
                .Append(new HtmlTag("td").Append(new HtmlTag("a").Attr("href", (errorCount > 0 ? "#errors" : string.Empty)).Text("Errors")))
                .Append(new HtmlTag("td").Text(errorCount.ToString())))
                .Append(new HtmlTag("br"));
        }

        private static HtmlTag BuildDifferences(ConcurrentBag<ComparisonResult> results)
        {
            var differencesTable = new HtmlTag("table")
                .Append(new HtmlTag("tr"))
                .Append(new HtmlTag("th").Text("Document Id"))
                .Append(new HtmlTag("th").Text("Idx value"))
                .Append(new HtmlTag("th").Text("Expected value"))
                .Append(new HtmlTag("th").Text("EDT value"));


            foreach (var comparisonResult in results)
            {
                var resultRow = new HtmlTag("tr")
                    .Append(new HtmlTag("td").Text(comparisonResult.DocumentId))
                    .Append(new HtmlTag("td").Text(comparisonResult.IdxValue))
                    .Append(new HtmlTag("td").Text(comparisonResult.IdxConvertedValue))
                    .Append(new HtmlTag("td").Text(comparisonResult.EdtValue));

                differencesTable.Append(resultRow);

            }

            return new HtmlTag("span").Text("Differences").Id("differences").Append(differencesTable); ;
        }

        private static HtmlTag BuildErrors(ConcurrentBag<ComparisonError> errors)
        {
            var errorTable =
               new HtmlTag("table")
                            .Append(new HtmlTag("tr"))
                            .Append(new HtmlTag("th").Text("Document Id"))
                            .Append(new HtmlTag("th").Text("Error"));


            foreach (var comparisonResult in errors)
            {
                var resultRow = new HtmlTag("tr")
                    .Append(new HtmlTag("td").Text(comparisonResult.DocumentId)
                    .Append(new HtmlTag("td").Text(comparisonResult.ErrorMessage)));

                errorTable.Append(resultRow);

            }

            return new HtmlTag("span").Text("Errors").Id("errors").Append(errorTable);
        }
    }
}
