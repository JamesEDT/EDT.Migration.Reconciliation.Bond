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
        private List<StandardMapping> _standardMappings;
        private NativeFileFinder nativeFileFinder;
        private int batchSize = 500;

        [SetUp]
        public void SetupComp()
        {
            _standardMappings = new StandardMapReader()
                .GetStandardMappings()
                .Where(x => !string.IsNullOrEmpty(x.EdtName) &&
                            !x.EdtName.Equals("UNMAPPED", StringComparison.InvariantCultureIgnoreCase) &&
                            x.IdxNames.Any())
                .ToList();

            _standardMappings.ForEach(x => _idxToEdtConversionServices.Add(x, new IdxToEdtConversionService(x, true)));

        }

        [Test]
        public void ValidateIdxFieldPopulation()
        {
            var populated = 0;
            var different = 0;
            var documentsInIdxButNotInEdt = 0;
            var documentsInEdtButNotInIdx = 0;
            var idxNoValue = 0;
            var unexpectedErrors = 0;
            var matched = 0;
            var totalSampled = 0;
            var emptyField = false;
            List<Framework.Models.IdxLoadFile.Document> _idxSample;

            ConcurrentDictionary<string, Dictionary<string, string[]>> _expectedValues;

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

                        _expectedValues = new ConcurrentDictionary<string, Dictionary<string, string[]>>();

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

                                _expectedValues.TryAdd(document.DocumentId, convertedValues);
                            });

                            //Get Edt Values
                            var docIDs = documents.Select(x => x.DocumentId).ToList();

                            //normal doc
                            var fake = new List<string>()
                            {
                                "396-31505-0-280",
                                "396-31506-0-1867"
                            };
                            var edtDocs = EdtDocumentRepository.GetDocuments(fake);

                            //normal doc
                            _standardMappings.ForEach(mapping =>
                            {
                                using (var expectedLog = new StreamWriter(Path.Combine(Settings.LogDirectory,
                                    $"{mapping.EdtName}_expected.txt"), true))
                                {
                                    using (var diffLog = new StreamWriter(Path.Combine(Settings.LogDirectory,
                                        $"{mapping.EdtName}_expected.txt"), true))
                                    {
                                        documents.ForEach(document =>
                                        {
                                            var expectedValues = _expectedValues[document.DocumentId][mapping.EdtName];

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
                                                        expectedLog.WriteLine(
                                                            $"\"{document.DocumentId}\"\t\"{string.Join(";", expectedValues).Replace("\"", "\"\"")}\"");
                                                        diffLog.WriteLine(
                                                            $"\"{document.DocumentId}\"\tAdditional\t\"{string.Join(";", edtAdditional).Replace("\"", "\"\"")}\"");
                                                        diffLog.WriteLine(
                                                            $"\"{document.DocumentId}\"\tUnmatched\t\"{string.Join(";", unmatched).Replace("\"", "\"\"")}\"");
                                                    }
                                                }
                                                else
                                                {
                                                    //no edt values error
                                                    diffLog.WriteLine(
                                                        $"\"{document.DocumentId}\"\tUnmatched\t\"Edt Had no values whilst IDX did\"");
                                                }
                                            }
                                            catch (KeyNotFoundException)
                                            {
                                                //unmigrated doc
                                                Test.Log(Status.Error, $"Unmigrated doc {document.DocumentId}");
                                                diffLog.WriteLine(
                                                    $"\"{document.DocumentId}\"\tUnmigrated doc");
                                            }
                                        
                                        });
                                    }
                                }
                            });

                            // multiValues

                            //do comp and output diffs

                            if (documents == null || !documents.Any()) break;

                            //do comparison
                        }
                    } while (documents?.Count > 0);
                }
            }
            catch (Exception e)
            {
                Test.Log(Status.Error, e);
                throw;
            }








            
        }


        private Dictionary<string, string> GetEdtFieldValues(StandardMapping mappingUnderTest, List<string> idxDocumentIds, IdxToEdtConversionService _idxToEdtConversionService)
        {
            if (mappingUnderTest.IsEmailField())
            {
                return GetEmailFieldValues(idxDocumentIds, mappingUnderTest.EdtName);
            }

            if ((!string.IsNullOrEmpty(mappingUnderTest.EdtType) &&
                 mappingUnderTest.EdtType.Equals("MultiValueList", StringComparison.InvariantCultureIgnoreCase)) ||
                (_idxToEdtConversionService.MappedEdtDatabaseColumnType.HasValue &&
                 _idxToEdtConversionService.MappedEdtDatabaseColumnType.Value == ColumnType.MultiValueList))
            {
                var allFieldValues = EdtDocumentRepository.GetMultiValueFieldValues(idxDocumentIds,
                    _idxToEdtConversionService.EdtColumnDetails.DisplayName);

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
                return (_idxToEdtConversionService.MappedEdtDatabaseColumnType.HasValue &&
                        _idxToEdtConversionService.MappedEdtDatabaseColumnType == ColumnType.Date)
                    ? EdtDocumentRepository.GetDocumentDateField(idxDocumentIds,
                        _idxToEdtConversionService.MappedEdtDatabaseColumn)
                    : EdtDocumentRepository.GetDocumentField(idxDocumentIds,
                        _idxToEdtConversionService.MappedEdtDatabaseColumn);
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