using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Logging;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using Edt.Bond.Migration.Reconciliation.Suite.Validators;
using MoreLinq;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Document = Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile.Document;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
    [Category("IdxDocComparison")]
    [Category("Full")]
    [Description(
        "Compare Idx field with Edt Database field to validate implementation of mapping, for a subset of records.")]
    public class IdxDocumentByEdtComparison
    {
        private readonly Dictionary<StandardMapping,IdxToEdtConversionService> _idxToEdtConversionServices = new Dictionary<StandardMapping, IdxToEdtConversionService>();
        private readonly Dictionary<StandardMapping, ComparisonTestResult> _comparisonTestResults = new Dictionary<StandardMapping, ComparisonTestResult>();
    
        private List<StandardMapping> _standardMappings;
        private NativeFileFinder _nativeFileFinder;
        private TagsValidator _tagsValidator;
        private LocationValidator _locationValidator;
        private NonMigratedEmsFolderValidator _nonMigratedEmsFolderValidator;


        [SetUp]
        public void SetupComp()
        {
            _standardMappings = new StandardMapReader()
                .GetStandardMappings()
                .Where(x => !string.IsNullOrEmpty(x.EdtName) &&
                            !x.EdtName.Equals("UNMAPPED", StringComparison.InvariantCultureIgnoreCase) &&
                            x.IdxNames.Any())
                //.Where(x => x.EdtName.Equals("Corrected Date Request"))
                .ToList();
                

            _standardMappings.ForEach(x =>
            {
                _idxToEdtConversionServices.Add(x, new IdxToEdtConversionService(x, true));
                _comparisonTestResults.Add(x, new ComparisonTestResult(x));
            });

            _tagsValidator = new TagsValidator();
            _locationValidator = new LocationValidator();
            _nonMigratedEmsFolderValidator = new NonMigratedEmsFolderValidator();

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
                            documents
                                .AsParallel()
                                .ForEach(document =>
                            { 
                                var convertedValues = new Dictionary<string, string[]>();

                                _standardMappings.ForEach(mapping =>
                                {
                                    var idxValues = document.GetValuesForIdolFields(mapping.IdxNames);
                                    convertedValues.Add(mapping.EdtName,
                                        idxValues
                                        .Select(x => _idxToEdtConversionServices[mapping].ConvertValueToEdtForm(x).Trim())
                                        .ToArray());
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

                                var edtDbLookUpName = mapping.EdtType.Equals("MultiValueList", StringComparison.InvariantCultureIgnoreCase) ?
                                mapping.EdtName
                                : (_idxToEdtConversionServices[mapping].MappedEdtDatabaseColumn ?? mapping.EdtName);

                                bool isFieldAutoPopulatedIfNull = Settings.AutoPopulatedNullFields.Contains(mapping.EdtName);

                                var edtValuesForMapping = mapping.IsPartyField() || mapping.EdtType.Equals("MultiValueList", StringComparison.InvariantCultureIgnoreCase)
                                ? ConvertDictionaryToMappingDictionary(mapping.EdtName, GetEdtFieldValues(mapping, docIDs, _idxToEdtConversionServices[mapping]))
                                : edtDocs;


                                documents
                                .AsParallel()
                                .ForEach(document =>
                                {
                                    try
                                    {
                                        var expectedValues = expectedStandardValues[document.DocumentId][mapping.EdtName];


                                        var expectedString = (mapping.EdtType.Equals("Date")) && expectedValues.Any() ? expectedValues?.OrderBy(x => x).FirstOrDefault()
                                        : string.Join(";", expectedValues.Select(x => x.Trim()).OrderBy(x => x).Distinct()).Replace("\n\n", "\n").Replace("; ", ";");

                                        currentTestResult.TotalSampled++;
                                        try
                                        {

                                            string actual =
                                                edtValuesForMapping[document.DocumentId]?[edtDbLookUpName] ?? string.Empty;

                                            actual = actual.Replace("; ", ";").Replace("\n\n", "\n").Trim();

                                            if (!actual.Equals(expectedString, StringComparison.InvariantCultureIgnoreCase)
                                            && !(string.IsNullOrWhiteSpace(expectedString) && !string.IsNullOrWhiteSpace(actual) && isFieldAutoPopulatedIfNull))
                                            {

                                                if (mapping.EdtName.Equals("Host Document Id",
                                              StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                        //if .pst, check that null
                                                        var fileType = document.AllFields.FirstOrDefault(x => x.Key.Equals("FILETYPE_PARAMETRIC", StringComparison.InvariantCultureIgnoreCase));
                                                    if (fileType.Value.Equals(".pst", StringComparison.InvariantCultureIgnoreCase))
                                                    {
                                                        currentTestResult.Matched++;
                                                        currentTestResult.ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(document.DocumentId, "Host document id null due to being .pst"));
                                                    }
                                                    else
                                                    {
                                                        currentTestResult.Different++;
                                                        currentTestResult.ComparisonResults.Add(
                                                        new Framework.Models.Reporting.ComparisonResult(
                                                            document.DocumentId, string.Join(";", actual),
                                                            string.Join(";", expectedValues), string.Join(";", document.GetValuesForIdolFields(mapping.IdxNames))));
                                                    }
                                                }
                                                else if (mapping.EdtName.Equals("End Time", StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrWhiteSpace(expectedString))
                                                {
                                                }
                                                else if (mapping.EdtName.Equals("Start Time", StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrWhiteSpace(expectedString))
                                                {
                                                }
                                                else if (mapping.EdtName.Equals("File Extension", StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    if (_nativeFileFinder == null)
                                                        _nativeFileFinder = new NativeFileFinder();

                                                    var actualFileExtension = _nativeFileFinder.GetExtension(document.DocumentId) ?? ".txt";

                                                    if (actualFileExtension != null && actualFileExtension.Equals(".tif", StringComparison.InvariantCultureIgnoreCase))
                                                        actualFileExtension = ".txt";

                                                    if (actualFileExtension != null && actualFileExtension.Equals(actual, StringComparison.InvariantCultureIgnoreCase))
                                                    {
                                                        currentTestResult.Matched++;
                                                    }
                                                    else
                                                    {
                                                        currentTestResult.Different++;
                                                        currentTestResult.ComparisonResults.Add(new Framework.Models.Reporting.ComparisonResult(document.DocumentId, actual, expectedValues.FirstOrDefault(), string.Join("; ",
                                                            document.GetValuesForIdolFields(mapping.IdxNames))));

                                                    }
                                                }
                                                else if (mapping.IsPartyField())
                                                {
                                                        //get party record
                                                        var emailActual = string.Join(";", actual.Split(new char[] { ';', ',' }).Select(x => x.Trim()).Distinct().OrderBy(x => x.ToLower()));
                                                    var emailExpected = string.Join(";", expectedString.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(x => x.ToLower()));

                                                    if (emailActual.Equals(emailExpected, StringComparison.InvariantCultureIgnoreCase))
                                                    {
                                                        currentTestResult.Matched++;
                                                    }
                                                    else
                                                    {
                                                        currentTestResult.Different++;
                                                        currentTestResult.ComparisonResults.Add(new Framework.Models.Reporting.ComparisonResult(document.DocumentId, emailActual, emailExpected, string.Join("; ",
                                                            document.GetValuesForIdolFields(mapping.IdxNames))));

                                                    }
                                                }
                                                else if (mapping.EdtName.Equals("Recipient Email Domain", StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    if (expectedString.Length > 500)
                                                    {
                                                        currentTestResult.Matched++;
                                                    }
                                                    else
                                                    {
                                                    }

                                                }
                                                else if (mapping.EdtType.Equals("Date", StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    var gotActualDate = DateTime.TryParse(actual, out var actualDate);

                                                    var gotExpectedDate = DateTime.TryParse(expectedValues.First(), out var expectedDate);

                                                    if (gotActualDate && gotExpectedDate && actualDate.Equals(expectedDate) || (!gotActualDate && !gotExpectedDate))
                                                    {
                                                        currentTestResult.Matched++;
                                                    }
                                                    else
                                                    {
                                                        currentTestResult.Different++;

                                                        currentTestResult.AddComparisonResult(document.DocumentId,
                                                            string.Join("; ", actual), string.Join("; ", expectedValues),
                                                            string.Join("; ",
                                                                document.GetValuesForIdolFields(mapping.IdxNames)));
                                                    }

                                                }
                                                else
                                                {
                                                    expectedString = string.Join(";", expectedValues.Select(x => x.Trim()).Distinct()).Replace("\n\n", "\n").Replace("; ", ";");
                                                    var orderedExpectedString = string.Join(";", expectedValues.Select(x => x.Trim()).OrderBy(x => x).Distinct()).Replace("\n\n", "\n").Replace("; ", ";");

                                                    if (_idxToEdtConversionServices[mapping].EdtColumnDetails.Size.HasValue
                                                    && _idxToEdtConversionServices[mapping].EdtColumnDetails.Size > 100
                                                    && expectedString.Length > _idxToEdtConversionServices[mapping].EdtColumnDetails.Size.Value)
                                                    {
                                                        try
                                                        {
                                                            expectedString = expectedString.Substring(0, _idxToEdtConversionServices[mapping].EdtColumnDetails.Size.Value);
                                                        }
                                                        catch (Exception)
                                                        {
                                                            System.Console.WriteLine("");
                                                        }
                                                    }

                                                    if (!actual.Equals(expectedString, StringComparison.InvariantCultureIgnoreCase) && !actual.Equals(orderedExpectedString, StringComparison.InvariantCultureIgnoreCase))
                                                    {
                                                        currentTestResult.Different++;

                                                        currentTestResult.AddComparisonResult(document.DocumentId,
                                                            string.Join("; ", actual), string.Join("; ", expectedValues),
                                                            string.Join("; ",
                                                                document.GetValuesForIdolFields(mapping.IdxNames)));
                                                    }
                                                    else
                                                    {
                                                        currentTestResult.Matched++;
                                                    }
                                                }

                                            }
                                            else
                                            {
                                                currentTestResult.Matched++;

                                                if (!string.IsNullOrWhiteSpace(actual))
                                                    currentTestResult.Populated++;
                                            }

                                        }
                                        catch (KeyNotFoundException)
                                        {
                                            if (string.IsNullOrWhiteSpace(expectedString))
                                            {
                                                currentTestResult.Matched++;
                                            }
                                            else
                                            {
                                                    //unmigrated doc
                                                    currentTestResult.DocumentsInIdxButNotInEdt++;
                                                currentTestResult.AddComparisonResult(document.DocumentId, string.Empty, expectedString, string.Join(";", document.GetValuesForIdolFields(mapping.IdxNames)));
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        currentTestResult.AddComparisonError(document.DocumentId, $"Failed processing document {e.Message} {e.StackTrace}");
                                    }
                                });


                            });

                            //tags / locations
                            _tagsValidator.Validate(documents);
                            _locationValidator.Validate(documents);
                            _nonMigratedEmsFolderValidator.Validate(documents);

                            if (!documents.Any()) break;

                            //do comparison
                        }
                    } while (documents?.Count > 0 && !idxProcessingService.EndOfFile);
                }
            }
            catch (Exception e)
            {
                _comparisonTestResults.First().Value.AddComparisonError("Uncaught exception", $"{e.Message} {e.StackTrace}");
                throw;
            }
            finally
            {
                OutputIntoReport();
                _comparisonTestResults.ForEach(result => result.Value.PrintExpectedOutputFile(result.Key.EdtName));
            }
        }

        private void OutputIntoReport()
        {
            try
            {
                var outputBatch = 1;

                _comparisonTestResults
                    .OrderBy(x => x.Value.Different + x.Value.ComparisonErrors.Count)
                    .Batch(30)
                    .ForEach(batch =>
                    {
                        var testOutput = HtmlReport.Instance.CreateTest($"Idx vs Edt Field ({outputBatch})");

                        batch.ForEach(resultSet => resultSet.Value.PrintDifferencesAndResults(testOutput));
                        outputBatch++;
                    });
            }
            catch (Exception e)
            {
                DebugLogger.Instance.WriteLine($"{e.Message} - {e.StackTrace}");
            }

            try
            {
                var transformedTestsReporter = HtmlReport.Instance.CreateTest("Transformed Field validation", "Compare Idx field with Edt Database field to validate implementation of EDTs customised value generation.");

                _tagsValidator.PrintStats(transformedTestsReporter);
                _locationValidator.PrintStats(transformedTestsReporter);
                
            }
            catch (Exception e)
            {
                DebugLogger.Instance.WriteLine($"{e.Message} - {e.StackTrace}");
            }
        }

        private Dictionary<string, string> GetEdtFieldValues(StandardMapping mappingUnderTest, List<string> idxDocumentIds, IdxToEdtConversionService idxToEdtConversionService)
        {
            if (mappingUnderTest.IsPartyField())
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

        private Dictionary<string, string[]> GetEdtFieldValuesAsArray(StandardMapping mappingUnderTest, List<string> idxDocumentIds, IdxToEdtConversionService idxToEdtConversionService)
        {
            if (mappingUnderTest.IsPartyField())
            {
                return GetEmailFieldValuesAsArray(idxDocumentIds, mappingUnderTest.EdtName);
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
                        DocNumber = (string) group.Key,
                        Values = group.Select(x => (string) x.FieldValue).OrderBy(x => x)
                    });

                var dictionaryValues =
                    combinedValues.ToDictionary(x => (string)x.DocNumber, x => x.Values.ToArray());
                return dictionaryValues;
            }
            else
            {
                return (idxToEdtConversionService.MappedEdtDatabaseColumnType.HasValue &&
                        idxToEdtConversionService.MappedEdtDatabaseColumnType == ColumnType.Date)
                    ? EdtDocumentRepository.GetDocumentDateFieldAsArray(idxDocumentIds,
                        idxToEdtConversionService.MappedEdtDatabaseColumn)
                    : EdtDocumentRepository.GetDocumentFieldAsArray(idxDocumentIds,
                        idxToEdtConversionService.MappedEdtDatabaseColumn);
            }
        }

        private Dictionary<string, Dictionary<string,string>> ConvertDictionaryToMappingDictionary(string mapping, Dictionary<string,string> dictionaryValue)
        {
            var concurrentDic = new Dictionary<string, Dictionary<string, string>>();

            dictionaryValue.AsParallel().ForEach(x =>
            {
                var valueDictionary = new Dictionary<string, string> {{mapping, x.Value}};
                concurrentDic.Add(x.Key, valueDictionary);
            });

            return concurrentDic;

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

        private Dictionary<string, string[]> GetEmailFieldValuesAsArray(List<string> documentIds, string fieldType)
        {
            var type = fieldType.Replace(Settings.EmailFieldIdentifyingPrefix, string.Empty);

            var allFields = EdtDocumentRepository.GetDocumentCorrespondances(documentIds)
                .Where(x => x.CorrespondanceType.Equals(type, StringComparison.InvariantCultureIgnoreCase));

            var desiredParties = from field in allFields
                                 group field.PartyName by field.DocumentNumber
                into correspondants
                                 select new
                                 {
                                     DocumentId = correspondants.Key,
                                     Value = correspondants.ToList().OrderBy(x => x).ToArray()
                                 };

            return desiredParties.ToDictionary(x => (string)x.DocumentId, x => x.Value);
        }



    }
}