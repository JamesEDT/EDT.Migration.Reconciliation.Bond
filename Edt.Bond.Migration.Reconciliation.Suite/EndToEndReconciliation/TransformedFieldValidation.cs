using System;
using AventStack.ExtentReports.MarkupUtils;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Output;
using Edt.Bond.Migration.Reconciliation.Framework.Extensions;
using Edt.Bond.Migration.Reconciliation.Framework.Logging;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
    [Category("Transformed")]
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
        [Category("Tags")]
        [Description("Validate mapping and conversion of AUN_WORKBOOK folders to EDT Tags")]
        public void Tags()
        {
            long idxUnpopulated = 0;
            long edtUnexpectedlyPopulated = 0;
            long errors = 0;
            long matched = 0;
            long different = 0;

            var workbookRecords = AunWorkbookReader.Read();

            DebugLogger.Instance.WriteLine("workbookRecords");


            var allEdtTags = EdtDocumentRepository.GetDocumentTags(_idxDocumentIds);

            DebugLogger.Instance.WriteLine("allEdtTags");

            try
            {

                using (var writer = new StreamWriter(Path.Combine(Settings.ReportingDirectory, "idx_tags.csv")))
                {

                    DebugLogger.Instance.WriteLine("idx_tags.csv");

                    writer.WriteLine("DocId,Tag");

                    foreach (var idxRecord in _idxSample)
                    {
                        var aunWorkbookIds = idxRecord.AllFields.Where(x => x.Key.Equals("AUN_WORKBOOK_NUMERIC", StringComparison.InvariantCultureIgnoreCase));
                        var foundEdtValue = allEdtTags.TryGetValue(idxRecord.DocumentId, out var relatedEdTags);

                        DebugLogger.Instance.WriteLine("foundEdtValue");

                        var cleanedEdTags = foundEdtValue ? relatedEdTags?.Select(x => x?.ReplaceTagChars()).ToList() : new List<string>();

                        DebugLogger.Instance.WriteLine("cleanedEdTags");


                        if (!aunWorkbookIds.Any())
                        {

                            DebugLogger.Instance.WriteLine("aunWorkbookIds");

                            idxUnpopulated++;
                            ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxRecord.DocumentId,
                                "AUN_WORKBOOK_NUMERIC field was not present for idx record"));

                            if (relatedEdTags != null && relatedEdTags.Any())
                            {

                                DebugLogger.Instance.WriteLine("aunWorkbookIds2");


                                edtUnexpectedlyPopulated++;
                                ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxRecord.DocumentId,
                                    $"EDT has value(s) {(relatedEdTags != null ? string.Join(",", relatedEdTags) : string.Empty)} when Idx record had no value"));
                            }

                            continue;
                        }


                        DebugLogger.Instance.WriteLine("outputTags-pre");

                        var outputTags = aunWorkbookIds.Select(x => workbookRecords.SingleOrDefault(c => c.Id == x.Value)?.FullPathOutput).ToList();

                        DebugLogger.Instance.WriteLine("outputTags");

                        if (outputTags.Any())
                        {
                            writer.WriteLine($"{idxRecord.DocumentId},\"{(outputTags != null ? string.Join(";", outputTags) : string.Empty)}\"");

                            foreach (var aunWorkbookId in aunWorkbookIds)
                            {

                                DebugLogger.Instance.WriteLine("aunWorkbookId2");

                                var tag = workbookRecords.SingleOrDefault(c => c.Id == aunWorkbookId.Value);

                                if (tag != null)
                                {


                                    if (!foundEdtValue || (relatedEdTags != null && !relatedEdTags.Any(x =>
                                                               x != null && x.Equals(tag.FullPath,
                                                                   System.StringComparison.InvariantCultureIgnoreCase))))
                                    {
                                        different++;
                                        var edtLogValue = relatedEdTags != null ? string.Join(";", relatedEdTags) : "none found";

                                        ComparisonResults.Add(new Framework.Models.Reporting.ComparisonResult(idxRecord.DocumentId,
                                            edtLogValue, tag.FullPath, aunWorkbookId.Value.ToString()));
                                    }
                                    else
                                    {
                                        matched++;
                                    }
                                }
                                else
                                {
                                    errors++;
                                    ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxRecord.DocumentId,
                                        $"Couldnt convert aun workbook id {aunWorkbookId} to name"));
                                }
                            }

                        }
                        else
                        {
                            ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxRecord.DocumentId, "No tags exist for this document"));
                        }

                       
                    }
                }

                var diffFile = PrintComparisonTables("Tags");
                TestLogger.Info($"Difference and error details written to: <a href=\"{diffFile}\">{diffFile}</a>");

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
            catch(Exception e)
            {
                TestLogger.Error(e.Message);
                TestLogger.Error(e.StackTrace);
                TestLogger.Error(e.Source);
                if(e.InnerException != null)
                {
                    TestLogger.Error(e.InnerException.Message);
                    TestLogger.Error(e.InnerException.StackTrace);

                }

                Assert.Fail(e.Message);
            }
        }

		[Test]
        [Category("Locations")]
		[Description("Validate mapping and conversion of IDX Locations to EDT Locations")]
		public void Locations()
		{
			long idxUnpopulated = 0;
			long edtUnexpectedlyPopulated = 0;
			long errors = 0;
			long matched = 0;
			long different = 0;

			var allEdtLocations = EdtDocumentRepository.GetDocumentLocations(_idxDocumentIds);

            using (var locationFileWriter = new LocationFileWriter())
            {

                foreach (var idxDocument in _idxSample)
                {

                    allEdtLocations.TryGetValue(idxDocument.DocumentId, out var edtLocation);

                    if (string.IsNullOrWhiteSpace(edtLocation))
                    {
                        different++;
                        ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxDocument.DocumentId, "Location not present in EDT"));
                    }

                    string group = idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(Settings.LocationIdxFields[0])).Value;
                    string custodian = idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(Settings.LocationIdxFields[1])).Value;
                    string source = idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(Settings.LocationIdxFields[2])).Value;

                    string location = $@"{group?? string.Empty}\{custodian ?? string.Empty}\{source ?? string.Empty}";

                    idxDocument.AllFields.Where(c => c.Key.StartsWith(Settings.LocationIdxFields[3])).OrderBy(c => c.Key).ToList().ForEach(
                        c =>
                        {
                            if (!string.IsNullOrWhiteSpace(c.Value) && !c.Value.Contains(".msg:"))
                            {
                                location += @"\" + c.Value.Replace(":", "-");
                            }
                        });

                    locationFileWriter.OutputRecord(idxDocument.DocumentId, location);

                    if (!location.ReplaceTagChars().Equals(edtLocation.ReplaceTagChars(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        different++;
                        ComparisonResults.Add(new Framework.Models.Reporting.ComparisonResult(idxDocument.DocumentId, edtLocation, location, location));
                        
                    }
                    else
                    {
                        matched++;
                    }
                   
                }
            }

			var diffFile = PrintComparisonTables("Locations");
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

		}

        [Test]
        [Category("TagList")]
        [Description("Idenfity unmigrated tags from aun workbook.csv")]
        public void NonMigratedEmsFoldersToTags()
        {
            var workbookRecords = AunWorkbookReader.Read();

            DebugLogger.Instance.WriteLine("workbookRecords");

            //var allDistinctEdtTags = EdtDocumentRepository.GetDocumentTags(_idxDocumentIds).SelectMany(x => x.Value)
            //                            .Distinct().ToList();

            var allIdxDistinctTags = _idxSample.SelectMany(x => x.AllFields.Where(field => field.Key.Equals("AUN_WORKBOOK_NUMERIC", StringComparison.InvariantCultureIgnoreCase)))
                                                .Select(x => x.Value)
                                                .Distinct()
                                                .ToList();

            var workbookRecordIds = workbookRecords.Select(x => x.Id).ToList();

            foreach (var idxTag in allIdxDistinctTags)
            {
                var relevantWorkbookRecord = workbookRecords.SingleOrDefault(x => x.Id == idxTag);
                if (relevantWorkbookRecord != null)
                {
                    workbookRecordIds.Remove(idxTag);
                    workbookRecordIds.RemoveAll(x => relevantWorkbookRecord.FullTagHierarchy.Contains(x));
                }
            }

            using (var sw = new StreamWriter(Path.Combine(Settings.ReportingDirectory, "nonidx_workbookRecords.csv")))
            {
                workbookRecordIds.ForEach(x => {
                    var workbookRecord = workbookRecords.SingleOrDefault(record => record.Id == x);
                    sw.WriteLine($"{workbookRecord.Id}\t{workbookRecord.Name}\t{workbookRecord.FullPath}");
                });
            }

        }

    }
}
