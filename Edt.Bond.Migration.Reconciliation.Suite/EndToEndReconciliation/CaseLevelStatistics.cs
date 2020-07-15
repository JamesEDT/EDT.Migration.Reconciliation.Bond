using System;
using AventStack.ExtentReports.MarkupUtils;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Services;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
    [Description("Case level statistics output at time of running the validation tool")]
    //[Parallelizable(ParallelScope.Children)]
    public class CaseLevelStatistics : TestBase
    {        

        [Test]
        [Description("Snapshot of document counts per ingest batch within EDT")]
        public void CaseLevelCounts()
        {
            var EdtDocumentCounts = EdtDocumentRepository.GetDocumentCountPerBatch();

            var tableData = new List<string[]>()
            {
                 new string[]{ "Batch", "Count of Documents"}
            };

            tableData.AddRange(EdtDocumentCounts.Select(x => new string[] { x.BatchName, x.DocumentCount.ToString() }));

            tableData.Add(new string[] { "Total", EdtDocumentCounts.Sum(x => x.DocumentCount).ToString()});

            Test.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(tableData.ToArray()));    
        }

        [Test]
        [Description("Snapshot of case size within EDT")]
        public void CaseLevelSizes()
        {
            var counterTokens = Settings.EdtImporterDatasetName.Split("_".ToCharArray()).Last().ToLower().Split("of".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (counterTokens.First() != counterTokens.Last())
            {
                Test.Log(AventStack.ExtentReports.Status.Info, "Statistics only generated in report for last idx of case.");
            }
            else
            {
                var EdtDocumentCounts = EdtDocumentRepository.GetDocumentCountPerBatch();

                var tableData = new List<string[]>()
            {
                new string[]{ "Item", "Size"}
            };

                var databaseStats = EdtDocumentRepository.GetDatabaseStats();

                tableData.AddRange(databaseStats.Select(stat => new string[] { stat.physical_name, $"{Math.Round(stat.size_kb / 1024, 1)} Mb" }));

                tableData.Add(new string[] { "Cfs Size", EdtCfsService.GetCaseSize() });
                tableData.Add(new string[] { "Source Size", $"{GetSizeOfSourceFolder()} Mb" });

                Test.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(tableData.ToArray()));
            }
        }


        private decimal GetSizeOfSourceFolder()
        {
            var sourceDir = new FileInfo(Settings.SourceFolderPath).DirectoryName;

            var totalBytes =
                Directory
                    .GetFiles(sourceDir, "*.*", SearchOption.AllDirectories)
                    .Sum(x => new FileInfo(x).Length);

            var inMb = ((decimal)totalBytes / 1024) / 1024;

            return Math.Round(inMb, 1);
        }
    }
}
