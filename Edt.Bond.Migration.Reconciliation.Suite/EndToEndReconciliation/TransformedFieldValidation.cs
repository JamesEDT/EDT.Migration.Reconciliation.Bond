using AventStack.ExtentReports.MarkupUtils;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
    [Description("Compare Idx field with Edt Database field to validate implementation of EDTs customised value generation.")]

    public class TransformedFieldValidation : TestBase
    {
        private IEnumerable<Framework.Models.IdxLoadFile.Document> _idxSample;
        private List<string> _idxDocumentIds;

        [OneTimeSetUp]
        public void SetIdxSample()
        {
            _idxSample = new IdxDocumentsRepository().GetSample();

            _idxDocumentIds = _idxSample.Select(x => x.DocumentId).ToList();

            FeatureRunner.Log(AventStack.ExtentReports.Status.Info, $"{_idxSample.Count()} sampled from Idx records.");
        }

        [Test]
        [Description("Validate mapping and conversion of AUN_WORKBOOK folders to EDT Tags")]
        public void Tags()
        {
            long idxUnpopulated = 0;
            long edtUnexpectedlyPopulated = 0;
            long errors = 0;
            long matched = 0;
            long different = 0;
            

            var workbookRecords = AunWorkbookReader.Read();
            var allEdtTags = EdtDocumentRepository.GetDocumentTags(_idxDocumentIds);

            foreach(var idxRecord in _idxSample)
            {
                var aunWorkbookIds = idxRecord.AllFields.Where(x => x.Key.Equals("AUN_WORKBOOK_NUMERIC"));
                var foundEdtValue = allEdtTags.TryGetValue(idxRecord.DocumentId, out var relatedEdTags);

                if (aunWorkbookIds.Count() == 0)
                {
                    idxUnpopulated++;
                    ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxRecord.DocumentId, "AUN_WORKBOOK_NUMERIC field was not present for idx record"));

                    if (relatedEdTags != null && relatedEdTags.Count() > 0)
                    {
                        edtUnexpectedlyPopulated++;
                        ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxRecord.DocumentId, $"EDT has value(s) {string.Join(",", relatedEdTags)} when Idx record had no value"));
                    }

                    continue;
                }

                foreach (var aunWorkbookId in aunWorkbookIds)
                {
                    var foundWorkbookName = workbookRecords.TryGetValue(aunWorkbookId.Value, out var aunWorkbookName);

                    if (foundWorkbookName)
                    {
                        if (!foundEdtValue || (relatedEdTags != null && !relatedEdTags.Any(x => x != null && x.Equals(aunWorkbookName, System.StringComparison.InvariantCultureIgnoreCase))))
                        {
                            different++;
                            var edtLogValue = relatedEdTags != null ? string.Join(";", relatedEdTags) : "none found";

                            ComparisonResults.Add(new Framework.Models.Reporting.ComparisonResult(idxRecord.DocumentId, edtLogValue, aunWorkbookName, aunWorkbookId.Value.ToString()));
                        }
                        else
                        {
                            matched++;
                        }
                    }
                    else
                    {
                        errors++;
                        ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxRecord.DocumentId, $"Couldnt convert aun workbook id {aunWorkbookId} to name"));
                    }
                }
            }

            var diffFile = PrintComparisonTables("Tags");
            TestLogger.Info($"Difference and error details written to: <a href=\"{diffFile}\">Report\\{diffFile}</a>");

            //print table of stats
            string[][] data = new string[][]{
                new string[]{ "<b>Comparison Statistics:</b>"},
                new string[]{ "Statistic", "Count"},
                new string[] { "Differences", different.ToString() },
                new string[] { "Matched", matched.ToString() },
                new string[] { "Unexpected Errors", errors.ToString() },
                new string[] { "Edt document(s) incorrectly have a value when Idx is null", edtUnexpectedlyPopulated.ToString() },
                new string[] { "Idx document(s) not populated for field under test (and EDt is also null)", idxUnpopulated.ToString() } };

            TestLogger.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data));

            Assert.Zero(different, $"Differences were seen between expected value and actual value");
            Assert.Zero(edtUnexpectedlyPopulated, "Edt was found to have field populated for instances where Idx was null");
            Assert.Zero(errors, "Expected errors encountered during processing");

            if (idxUnpopulated > 0)
                TestLogger.Info($"The Idx was found to not have a value for field AUN_WORKBOOK_NUMERIC in {idxUnpopulated} documents/instances.");
        }
    }
}
