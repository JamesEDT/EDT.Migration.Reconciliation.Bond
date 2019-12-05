using AventStack.ExtentReports.MarkupUtils;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
    [Description("Case level statistics output at time of running the validation tool")]
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

            TestLogger.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(tableData.ToArray()));    
        }
    }
}
