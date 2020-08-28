using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Logging;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using Edt.Bond.Migration.Reconciliation.Suite.Validators;
using Edt.Bond.Migration.Reconciliation.Suite.Validators.FieldValidators;
using Microsoft.CodeAnalysis;
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
        private readonly Dictionary<StandardMapping, IdxToEdtConversionService> _idxToEdtConversionServices = new Dictionary<StandardMapping, IdxToEdtConversionService>();
        private readonly Dictionary<StandardMapping, ComparisonTestResult> _comparisonTestResults = new Dictionary<StandardMapping, ComparisonTestResult>();

        private List<StandardMapping> _standardMappings;
        private NativeFileFinder _nativeFileFinder;
        private AunWorkbookTagsValidator _tagsValidator;
        private LocationValidator _locationValidator;
        private NonMigratedEmsFolderValidator _nonMigratedEmsFolderValidator;
        private SubjectIssuesTagsValidator _subjectIssuesTagsValidator;


        [SetUp]
        public void SetupComp()
        {
            _standardMappings = new StandardMapReader()
                .GetStandardMappings()
                .Where(x => !string.IsNullOrEmpty(x.EdtName) &&
                            !x.EdtName.Equals("UNMAPPED", StringComparison.InvariantCultureIgnoreCase) &&
                            x.IdxNames.Any())
                //.Where(x => x.EdtName.Equals("InternetHeaders", StringComparison.InvariantCultureIgnoreCase))
                .ToList();


            _standardMappings.ForEach(x =>
            {
                _idxToEdtConversionServices.Add(x, new IdxToEdtConversionService(x, true));
                _comparisonTestResults.Add(x, new ComparisonTestResult(x));
            });

            _tagsValidator = new AunWorkbookTagsValidator();
            _locationValidator = new LocationValidator();
            _nonMigratedEmsFolderValidator = new NonMigratedEmsFolderValidator();
            _subjectIssuesTagsValidator = new SubjectIssuesTagsValidator();
            

        }

        [Test]
        public void ValidateIdxFieldPopulation()
        {
            ConcurrentDictionary<string, Dictionary<string, string[]>> expectedStandardValues;

            try
            {
                var idxPaths = GetIdxFilePaths();
                var edtDocColumns = EdtDocumentRepository.GetDocumentColumnNames().Where(X => !X.Equals("Body")).ToList();

                List<Document> allDocuments;


                foreach (var idxPath in idxPaths)
                {
                    var idxProcessingService = new IdxReaderByChunk(File.OpenText(idxPath));

                    do
                    {
                        allDocuments = idxProcessingService.GetNextDocumentBatch()?.ToList();

                        if (allDocuments != null)
                        {
                            allDocuments
                               // .Where(x => x.DocumentId != "RE00894-82702-002496")
                               .Batch(2000)
                               
                               .ForEach(ieDocuments =>
                               {
                                   var documents = ieDocuments.ToList();

                                   expectedStandardValues = new ConcurrentDictionary<string, Dictionary<string, string[]>>();

                                   if (documents != null)
                                   {

                                       //make expected values from IDX values
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
                                                   .Select(x => _idxToEdtConversionServices[mapping].ConvertValueToEdtForm(x)?.Trim())
                                                   .Where(x => !string.IsNullOrWhiteSpace(x))
                                                   .ToArray());
                                           });

                                           expectedStandardValues.TryAdd(document.DocumentId, convertedValues);
                                       });

                                       //Get Edt Values
                                       try
                                       {
                                           var docIDs = documents.Select(x => x.DocumentId).ToList();

                                           //normal doc
                                           Dictionary<string, Dictionary<string, string>> edtDocs = EdtDocumentRepository.GetDocuments(docIDs, edtDocColumns);

                                           _standardMappings.ForEach(mapping =>
                                           {
                                               var currentTestResult = _comparisonTestResults[mapping];

                                               var edtDbLookUpName = GetEdtFieldDictionaryKey(mapping);

                                               bool isFieldAutoPopulatedIfNull = Settings.AutoPopulatedNullFields.Contains(mapping.EdtName);

                                               var edtValuesForMapping = mapping.IsPartyField() || mapping.EdtType.Equals("MultiValueList", StringComparison.InvariantCultureIgnoreCase) || mapping.EdtName.Equals("Document Type", StringComparison.InvariantCultureIgnoreCase) || mapping.EdtName.Equals("Custodian", StringComparison.InvariantCultureIgnoreCase)
                                               ? ConvertDictionaryToMappingDictionary(mapping.EdtName, GetEdtFieldValues(mapping, docIDs, _idxToEdtConversionServices[mapping]))
                                               : edtDocs;


                                               documents
                                               .AsParallel()
                                               .ForEach(document =>
                                               {

                                                   try
                                                   {
                                                       var expectedValues = expectedStandardValues[document.DocumentId][mapping.EdtName];

                                                       var expectedString = (mapping.EdtType.Equals("Date")) && expectedValues.Any()
                                                       ? expectedValues?.OrderBy(x => x).FirstOrDefault()
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
                                                               ValidationResult validationResult;

                                                               if (mapping.EdtName.Equals("Host Document Id", StringComparison.InvariantCultureIgnoreCase))
                                                               {
                                                                   validationResult = HostDocumentIdValidator.Validate(document);
                                                               }
                                                               else if (mapping.EdtName.Equals("End Time", StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrWhiteSpace(expectedString))
                                                               {
                                                                   validationResult = new ValidationResult()
                                                                   {
                                                                       Matched = true
                                                                   };
                                                               }
                                                               else if (mapping.EdtName.Equals("Start Time", StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrWhiteSpace(expectedString))
                                                               {
                                                                   validationResult = new ValidationResult()
                                                                   {
                                                                       Matched = true
                                                                   };
                                                               }
                                                               else if (mapping.EdtName.Equals("File Extension", StringComparison.InvariantCultureIgnoreCase))
                                                               {
                                                                   validationResult = FileExtensionValidator.Validate(document, actual);
                                                               }
                                                               else if (mapping.EdtName.Equals("Author", StringComparison.InvariantCultureIgnoreCase))
                                                               {
                                                                   validationResult = PartyFieldValidator.Validate(mapping, document, expectedString, actual);
                                                               }
                                                               else if (mapping.IsPartyField())
                                                               {
                                                                   validationResult = PartyFieldValidator.Validate(mapping, document, expectedString, actual);
                                                               }
                                                               else if (mapping.EdtName.Equals("Recipient Email Domain", StringComparison.InvariantCultureIgnoreCase))
                                                               {                                                                  
                                                                   validationResult = RecipientEmailDomanValidator.Validate(document, expectedString, actual);
                                                               }
                                                               else if (mapping.EdtName.Equals("Ems Sent Date", StringComparison.InvariantCultureIgnoreCase))
                                                               {
                                                                   validationResult = new ValidationResult()
                                                                   {
                                                                       Matched = actual.Equals(expectedString, StringComparison.InvariantCultureIgnoreCase),
                                                                       ExpectedComparisonValue = expectedString,
                                                                       EdtComparisonValue = actual
                                                                   };
                                                               }
                                                               else if (mapping.EdtType.Equals("Date", StringComparison.InvariantCultureIgnoreCase))
                                                               {
                                                                   validationResult = DateFieldValidator.Validate(document, mapping, expectedValues, actual);
                                                               }
                                                               else if (mapping.EdtType.Trim().Equals("MultiValueList", StringComparison.InvariantCultureIgnoreCase))
                                                               {
                                                                   validationResult = MultiValueListFieldValidator.Validate(document, expectedValues, actual);
                                                               }
                                                               else if (mapping.EdtName == "EMS Recipients" || mapping.EdtName.Equals("All Email Addresses", StringComparison.InvariantCultureIgnoreCase))
                                                               {
                                                                   validationResult = CustomEmailFieldValidator.Validate(document, expectedValues, actual);
                                                               }
                                                               else if (mapping.EdtName == "Page Count")
                                                               {
                                                                   validationResult = PageCountValidator.Validate(document, expectedString, actual);
                                                               }
                                                               else
                                                               {
                                                                   //generic comparison
                                                                   actual = actual.Replace("\r\n", "\n");
                                                               expectedString = string.Join(";", expectedValues.Select(x => x.Trim()).Distinct()).Replace("\r\n","\n").Replace("\n\n", "\n").Replace("; ", ";");
                                                                   var orderedExpectedString = string.Join(";", expectedValues.Select(x => x.Trim()).OrderBy(x => x).Distinct()).Replace("\r\n", "\n").Replace("\n\n", "\n").Replace("\n\n", "\n").Replace("; ", ";");

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
                                                                       }
                                                                   }

                                                                   validationResult = new ValidationResult()
                                                                   {
                                                                       Matched = !(!actual.Equals(expectedString, StringComparison.InvariantCultureIgnoreCase) && !actual.Equals(orderedExpectedString, StringComparison.InvariantCultureIgnoreCase)),
                                                                       ExpectedComparisonValue = orderedExpectedString,
                                                                       EdtComparisonValue = actual
                                                                   };
                                                               }

                                                               if (validationResult != null && validationResult.Matched)
                                                               {
                                                                   currentTestResult.Matched++;
                                                               }
                                                               else
                                                               {
                                                                   currentTestResult.Different++;
                                                                   currentTestResult.AddComparisonResult(document.DocumentId, validationResult.EdtComparisonValue, validationResult.ExpectedComparisonValue, string.Join(";", document.GetValuesForIdolFields(mapping.IdxNames)));
                                                               }

                                                               if (validationResult.IsError)
                                                                   currentTestResult.AddComparisonError(document.DocumentId, validationResult.ErrorMessage);
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
                                                       currentTestResult.AddComparisonError(document.DocumentId, $"Failed processing document {e.Message}, {e.StackTrace}");
                                                   }
                                               });


                                           });

                                           //tags / locations
                                           _tagsValidator.Validate(documents);
                                           _subjectIssuesTagsValidator.Validate(documents);
                                           _locationValidator.Validate(documents);
                                           _nonMigratedEmsFolderValidator.Validate(documents);

                                           if (!documents.Any()) return;

                                       }
                                       catch(Exception ex)
                                       {
                                           _comparisonTestResults.First().Value.AddComparisonError("Uncaught exception", $"{ex.Message} {ex.StackTrace}");
                                       }
                                   }
                               });
                        }
                    } while (allDocuments?.Count > 0 || !idxProcessingService.EndOfFile);
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

        private string GetEdtFieldDictionaryKey(StandardMapping mapping)
        {
            if (mapping.EdtType.Trim().Equals("MultiValueList", StringComparison.InvariantCultureIgnoreCase))
            {
                return mapping.EdtName;
            }
            else if (mapping.EdtName.Trim().Equals("Custodian", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Custodian";
            }
            else if (mapping.EdtName.Trim().Equals("Document Type", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Document Type";
            }
            else
            {
                return _idxToEdtConversionServices[mapping].MappedEdtDatabaseColumn ?? mapping.EdtName;
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
                    combinedValues.ToDictionary(x => (string)x.DocNumber, x => string.Join(";", x.Values));
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
                        DocNumber = (string)group.Key,
                        Values = group.Select(x => (string)x.FieldValue).OrderBy(x => x)
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

        private Dictionary<string, Dictionary<string, string>> ConvertDictionaryToMappingDictionary(string mapping, Dictionary<string, string> dictionaryValue)
        {
            var concurrentDic = new Dictionary<string, Dictionary<string, string>>();

            dictionaryValue.AsParallel().ForEach(x =>
            {
                var valueDictionary = new Dictionary<string, string> { { mapping, x.Value } };
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
                                     DocumentId = correspondants.Key,
                                     Value = string.Join(";", correspondants.ToList().OrderBy(x => x))
                                 };

            return desiredParties.ToDictionary(x => (string)x.DocumentId, x => x.Value);
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

        private static string[] GetIdxFilePaths()
        {
            if (Directory.Exists(Settings.IdxFilePath))
            {
                return Directory.GetFiles(Settings.IdxFilePath, "*.idx");

            }
            else if (File.Exists(Settings.IdxFilePath))
            {
                return new string[] { Settings.IdxFilePath };
            }

            throw new ArgumentException("Cannot determine if IdxFilePath is directory or single idx");
        }

    }
}