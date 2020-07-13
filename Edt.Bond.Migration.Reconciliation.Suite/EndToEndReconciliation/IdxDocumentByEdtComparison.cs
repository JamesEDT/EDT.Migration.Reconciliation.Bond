using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Logging;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Exceptions;
using Edt.Bond.Migration.Reconciliation.Framework.Output;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using MoreLinq;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AventStack.ExtentReports;
using Document = Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile.Document;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
    [Category("IdxDocComparison")]
    [Description(
        "Compare Idx field with Edt Database field to validate implementation of mapping, for a subset of records.")]
    public class IdxDocumentByEdtComparison : TestBase
    {
        private Dictionary<StandardMapping,IdxToEdtConversionService> _idxToEdtConversionServices = new Dictionary<StandardMapping, IdxToEdtConversionService>();
        private Dictionary<StandardMapping, ComparisonTestResult> _comparisonTestResults = new Dictionary<StandardMapping, ComparisonTestResult>();
        private List<StandardMapping> _standardMappings;
        private NativeFileFinder _nativeFileFinder;
        private int _batchSize = 500;

        [SetUp]
        public void SetupComp()
        {
            _standardMappings = new StandardMapReader()
                .GetStandardMappings()
                .Where(x => !string.IsNullOrEmpty(x.EdtName) &&
                            !x.EdtName.Equals("UNMAPPED", StringComparison.InvariantCultureIgnoreCase) &&
                            x.IdxNames.Any())
                .ToList();

            _standardMappings.ForEach(x =>
            {
                _idxToEdtConversionServices.Add(x, new IdxToEdtConversionService(x, true));
                _comparisonTestResults.Add(x, new ComparisonTestResult(x.EdtName));
            });

        }

        [Test]
        public void ValidateIdxFieldPopulation()
        {
            ConcurrentDictionary<string, Dictionary<string, string[]>> expectedStandardValues;

            try
            {
                var idxPaths = Settings.IdxFilePath.Split(new char[] { '|' });

                List<Document> documents;


                foreach (var idxPath in idxPaths)
                {
                    var idxProcessingService = new IdxReaderByChunk(File.OpenText(idxPath));

                    do
                    {
                        documents = idxProcessingService.GetNextDocumentBatch()?.ToList();

                        expectedStandardValues = new ConcurrentDictionary<string, Dictionary<string, string[]>>();

                        if (documents != null)
                        {
                            documents.AsParallel().ForEach(document =>
                            {
                                var convertedValues = new Dictionary<string, string[]>();

                                _standardMappings.ForEach(mapping =>
                                {
                                    var idxValues = document.GetValuesForIdolFields(mapping.IdxNames);
                                    convertedValues.Add(mapping.EdtName, idxValues.Select(x =>
                                        _idxToEdtConversionServices[mapping].ConvertValueToEdtForm(x)).ToArray());
                                });

                                expectedStandardValues.TryAdd(document.DocumentId, convertedValues);
                            });

                            //Get Edt Values
                            var docIDs = documents.Select(x => x.DocumentId).ToList();

                            //normal doc
                            var edtDocs = EdtDocumentRepository.GetDocuments(docIDs);

                            _standardMappings.ForEach(mapping =>
                            {
                                var currentTestResult = _comparisonTestResults[mapping];

                                using (var expectedLog = new StreamWriter(Path.Combine(Settings.LogDirectory,
                                    $"{mapping.EdtName}_expected_raw.txt"), true))
                                {

                                    documents.ForEach(document =>
                                    {
                                        var expectedValues =
                                            expectedStandardValues[document.DocumentId][mapping.EdtName];
                                        currentTestResult.TotalSampled++;
                                        try
                                        {

                                            var actual =
                                                edtDocs[document.DocumentId]?[
                                                        _idxToEdtConversionServices[mapping]
                                                            .MappedEdtDatabaseColumn]
                                                    ?.Split(";".ToCharArray(),
                                                        StringSplitOptions.RemoveEmptyEntries);

                                            if (actual != null)
                                            {
                                                var unmatched = expectedValues.Except(actual).ToList();
                                                var edtAdditional = actual.Except(expectedValues).ToList();

                                                if (unmatched.Any() || edtAdditional.Any())
                                                {
                                                    currentTestResult.Different++;

                                                    if (!expectedValues.Any() && actual.Any())
                                                        currentTestResult.DocumentsInEdtButNotInIdx++;


                                                    expectedLog.WriteLine(
                                                        $"\"{document.DocumentId}\"\t\"{string.Join(";", expectedValues).Replace("\"", "\"\"")}\"");

                                                    currentTestResult.AddComparisonResult(document.DocumentId,
                                                        string.Join("; ", actual), string.Join("; ", expectedValues),
                                                        string.Join("; ",
                                                            document.GetValuesForIdolFields(mapping.IdxNames)));
                                                }
                                                else
                                                {
                                                    currentTestResult.Matched++;

                                                    if (actual.Any())
                                                        currentTestResult.Populated++;
                                                }
                                            }
                                            else
                                            {
                                                currentTestResult.AddComparisonError(document.DocumentId,
                                                    "Edt had no values whilst IDX was populated");
                                                currentTestResult.DocumentsInIdxButNotInEdt++;
                                            }
                                        }
                                        catch (KeyNotFoundException)
                                        {
                                            //unmigrated doc
                                            currentTestResult.DocumentsInIdxButNotInEdt++;
                                            currentTestResult.AddComparisonError(document.DocumentId, $"Unmigrated doc {document.DocumentId}");
                                        }

                                    });

                                }
                            });

                            // multiValues

                            //tags / locations

                            if (documents == null || !documents.Any()) break;

                            //do comparison
                        }
                    } while (documents?.Count > 0);
                    _comparisonTestResults.ForEach(result => result.Value.PrintDifferencesAndResults(Test));
                    _comparisonTestResults.ForEach(result => result.Value.PrintExpectedOutputFile(result.Key.EdtName));
                }
            }
            catch (Exception e)
            {
                Test.Log(Status.Error, e);
                throw;
            }








            
        }


        private Dictionary<string, string> GetEdtFieldValues(StandardMapping mappingUnderTest, List<string> idxDocumentIds, IdxToEdtConversionService idxToEdtConversionService)
        {
            if (mappingUnderTest.IsEmailField())
            {
                return GetEmailFieldValues(idxDocumentIds, mappingUnderTest.EdtName);
            }

            if ((!string.IsNullOrEmpty(mappingUnderTest.EdtType) &&
                 mappingUnderTest.EdtType.Equals("MultiValueList", StringComparison.InvariantCultureIgnoreCase)) ||
                (idxToEdtConversionService.MappedEdtDatabaseColumnType.HasValue &&
                 idxToEdtConversionService.MappedEdtDatabaseColumnType.Value == ColumnType.MultiValueList))
            {
                var allFieldValues = EdtDocumentRepository.GetMultiValueFieldValues(idxDocumentIds,
                    idxToEdtConversionService.EdtColumnDetails.DisplayName);

                var combinedValues = allFieldValues.GroupBy(x => x.DocNumber)
                    .Select(group => new
                    {
                        DocNumber = group.Key,
                        Values = group.Select(x => x.FieldValue).OrderBy(x => x)
                    });

                var dictionaryValues =
                    combinedValues.ToDictionary(x => (string) x.DocNumber, x => string.Join(";", x.Values));
                return dictionaryValues;
            }
            else
            {
                return (idxToEdtConversionService.MappedEdtDatabaseColumnType.HasValue &&
                        idxToEdtConversionService.MappedEdtDatabaseColumnType == ColumnType.Date)
                    ? EdtDocumentRepository.GetDocumentDateField(idxDocumentIds,
                        idxToEdtConversionService.MappedEdtDatabaseColumn)
                    : EdtDocumentRepository.GetDocumentField(idxDocumentIds,
                        idxToEdtConversionService.MappedEdtDatabaseColumn);
            }
        }

        private Dictionary<string, string> GetEmailFieldValues(List<string> documentIds, string fieldType)
        {
            var type = fieldType.Replace(Settings.EmailFieldIdentifyingPrefix, string.Empty);

            var allFields = EdtDocumentRepository.GetDocumentCorrespondances(documentIds)
                .Where(x => x.CorrespondanceType.Equals(type, StringComparison.InvariantCultureIgnoreCase));

            var desiredParties = from field in allFields
                group field.PartyName by field.DocumentNumber
                into correspondants
                select new
                {
                    DocumentId = correspondants.Key, Value = string.Join(";", correspondants.ToList().OrderBy(x => x))
                };

            return desiredParties.ToDictionary(x => (string) x.DocumentId, x => x.Value);
        }

    
    }
}