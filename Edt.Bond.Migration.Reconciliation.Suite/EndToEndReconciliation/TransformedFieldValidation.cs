using AventStack.ExtentReports;
using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Extensions;
using Edt.Bond.Migration.Reconciliation.Framework.Logging;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using Edt.Bond.Migration.Reconciliation.Framework.Output;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
    [Category("Transformed")]
    [Description("Compare Idx field with Edt Database field to validate implementation of EDTs customised value generation.")]
    [Ignore("replaced within inline")]
    public class TransformedFieldValidation : ComparisonTest
    {
        private List<Framework.Models.IdxLoadFile.Document> _idxSample;
        private List<string> _idxDocumentIds;

        [OneTimeSetUp]
        public void SetIdxSample()
        {
            _idxSample = new IdxDocumentsRepository().GetSample().ToList();

            _idxDocumentIds = _idxSample.Select(x => x.DocumentId).ToList();

            TestSuite.Log(AventStack.ExtentReports.Status.Info, $"{_idxSample.Count()} sampled from Idx records.");
        }

        [Test]
        [Category("Tags")]
        [Description("Validate mapping and conversion of AUN_WORKBOOK folders to EDT Tags")]
        public void Tags()
        {
            EdtFieldUnderTest = "Tags";
            long idxUnpopulated = 0;
            long edtUnexpectedlyPopulated = 0;
            long errors = 0;
            long matched = 0;
            long different = 0;

            var workbookRecords = AunWorkbookReader.Read();

            DebugLogger.Instance.WriteLine("workbookRecords");

            var unmigratedTags = new string[] { "Ems Folders:Deleted Items", "Ems Folders:15. LPP/Protected Review (and Subfolders)" };


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
                        var aunWorkbookIds = idxRecord.AllFields.Where(x => x.Key.Equals("AUN_WORKBOOK_NUMERIC", StringComparison.InvariantCultureIgnoreCase)).ToList();
                        var foundEdtValue = allEdtTags.TryGetValue(idxRecord.DocumentId, out var relatedEdTags);

                        DebugLogger.Instance.WriteLine("foundEdtValue");

                        var cleanedEdTags = foundEdtValue ? relatedEdTags?.Select(x => x?.ReplaceTagChars()).ToList() : new List<string>();

                        DebugLogger.Instance.WriteLine("cleanedEdTags");


                        if (!aunWorkbookIds.Any())
                        {

                            DebugLogger.Instance.WriteLine("aunWorkbookIds");

                            idxUnpopulated++;
                            AddComparisonError(idxRecord.DocumentId,
                                "AUN_WORKBOOK_NUMERIC field was not present for idx record");

                            if (relatedEdTags != null && relatedEdTags.Any()) //subject issues
                            {

                                DebugLogger.Instance.WriteLine("aunWorkbookIds2");

                                edtUnexpectedlyPopulated++;
                                AddComparisonError(idxRecord.DocumentId,
                                    $"EDT has value(s) {string.Join(",", relatedEdTags)} when Idx record had no value");
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
                                    if (unmigratedTags.Contains(tag.FullPath))
                                    {
                                        if(foundEdtValue && relatedEdTags.Any(x => x != null && x.Equals(tag.FullPath,
                                                                       System.StringComparison.InvariantCultureIgnoreCase)))
                                        {
                                            different++;
                                            var edtLogValue = relatedEdTags != null ? string.Join(";", relatedEdTags) : "none found";

                                            ComparisonResults.Add(new Framework.Models.Reporting.ComparisonResult(idxRecord.DocumentId,
                                                edtLogValue, $"{tag.FullPath} not to be migrated", aunWorkbookId.Value.ToString()));

                                        }
                                        else
                                        {
                                            matched++;
                                        }
                                    }
                                    else
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
                                }
                                else
                                {
                                    errors++;
                                    AddComparisonError(idxRecord.DocumentId, $"Couldnt convert aun workbook id {aunWorkbookId} to name");
                                }
                            }

                        }
                        else
                        {
                            AddComparisonError(idxRecord.DocumentId, "No tags exist for this document");
                        }

                       
                    }
                }
                
                //print table of stats
                LogComparisonStatistics( new string[][]{
                    new string[] { "Differences", different.ToString() },
                    new string[] { "Matched", matched.ToString() },
                    new string[] { "Unexpected Errors", errors.ToString() },
                    new string[] { "Edt document(s) incorrectly have a value when Idx is null", edtUnexpectedlyPopulated.ToString() },
                    new string[] { "Idx document(s) not populated for field under test (and EDt is also null)", idxUnpopulated.ToString() }
                });


                Assert.Zero(different, $"Differences were seen between expected value and actual value");
                Assert.Zero(edtUnexpectedlyPopulated, "Edt was found to have field populated for instances where Idx was null");
                Assert.Zero(errors, "Expected errors encountered during processing");

                if (idxUnpopulated > 0)
                    Test.Info($"The Idx was found to not have a value for field AUN_WORKBOOK_NUMERIC in {idxUnpopulated} documents/instances.");
            }
            catch(Exception e)
            {
                Test.Log(Status.Error, e);

                Assert.Fail(e.Message);
            }
        }

		[Test]
        [Category("Locations")]
		[Description("Validate mapping and conversion of IDX Locations to EDT Locations")]
		public void Locations()
		{
            EdtFieldUnderTest = "Locations";
            long idxUnpopulated = 0;
			long edtUnexpectedlyPopulated = 0;
			long errors = 0;
			long matched = 0;
			long different = 0;
            List<EmsFolder> observedEmsFolders = new List<EmsFolder>();
            var textSegment = Settings.LocationIdxFields[3];


            var allEdtLocations = EdtDocumentRepository.GetDocumentLocations(_idxDocumentIds);

            using (var locationFileWriter = new LocationFileWriter())
            {
                foreach (var idxDocument in _idxSample)
                {

                    allEdtLocations.TryGetValue(idxDocument.DocumentId, out var edtLocation);

                    if (string.IsNullOrWhiteSpace(edtLocation))
                    {
                        different++;
                        AddComparisonError(idxDocument.DocumentId, "Location not present in EDT");
                    }

                    var emsFolder = new EmsFolder()
                    {
                        Group = (idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(Settings.LocationIdxFields[0])) ?? idxDocument.AllFields.SingleOrDefault(x =>
                        x.Key.Equals("EMS DocLibrary Group", StringComparison.InvariantCultureIgnoreCase)))?.Value,
                        Custodian = (idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(Settings.LocationIdxFields[1])) ?? idxDocument.AllFields.SingleOrDefault(x =>
                        x.Key.Equals("EMS DocLibrary Custodian", StringComparison.InvariantCultureIgnoreCase)))?.Value,
                        Source = (idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(Settings.LocationIdxFields[2])) ?? idxDocument.AllFields.SingleOrDefault(x =>
                        x.Key.Equals("EMS DocLibrary Source", StringComparison.InvariantCultureIgnoreCase)))?.Value
                    };

                    for (var i = 1; i < 30; i++)
                    {
                        //idxDocument.AllFields.Where(c => c.Key.StartsWith(Settings.LocationIdxFields[3])).OrderBy(c => c.Key).ToList().ForEach(
                        //    c =>
                        //     {
                        var segment = idxDocument.AllFields.SingleOrDefault(c => c.Key.Equals($"{textSegment}{i}"))?.Value;

                        if (!string.IsNullOrWhiteSpace(segment) && !segment.Contains(".msg:"))
                        {
                            emsFolder.VIRTUAL_PATH_SEGMENTs.Add(segment.Replace(":", "-"));
                        }
                     //       });
                    }

                    observedEmsFolders.Add(emsFolder);
                    locationFileWriter.OutputRecord(idxDocument.DocumentId, emsFolder.ConvertedEdtLocation);

                    if (!emsFolder.ConvertedEdtLocation.ReplaceTagChars().Equals(edtLocation.ReplaceTagChars(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        different++;
                        AddComparisonResult(idxDocument.DocumentId, edtLocation, emsFolder.ConvertedEdtLocation, emsFolder.ConvertedEdtLocation);
                    }
                    else
                    {
                        matched++;
                    }
                   
                }
            }

            PrintObservedLocations(observedEmsFolders);

            //print table of stats
            LogComparisonStatistics(new string[][]
            {
                new string[] {"Differences", different.ToString()},
                new string[] {"Matched", matched.ToString()}
            });

            Assert.Zero(different, $"Differences were seen between expected value and actual value");

		}

        private void PrintObservedLocations(IEnumerable<EmsFolder> emsFolders)
        {
            using (var writer = new StreamWriter(Path.Combine(Settings.ReportingDirectory, "locations_observedIdxRawValues.csv")))
            {
                writer.WriteLine("Group, Custodians, Source, Virtual Path Segments");

                emsFolders
                    .Distinct()
                    .ToList()
                    .ForEach(x => writer.WriteLine($"{x.Group},{x.Custodian},{x.Source},{x.VirtualPathSegements}"));
            }
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
