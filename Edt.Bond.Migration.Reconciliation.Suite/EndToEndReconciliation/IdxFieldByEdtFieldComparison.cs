using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Logging;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Exceptions;
using Edt.Bond.Migration.Reconciliation.Framework.Output;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MoreLinq;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
    [Category("IdxComparison")]
    [Description(
        "Compare Idx field with Edt Database field to validate implementation of mapping, for a subset of records.")]
    public class IdxFieldByEdtFieldComparison : ComparisonTest
    {
        private IEnumerable<Framework.Models.IdxLoadFile.Document> _idxSample;
        private List<string> _idxDocumentIds;
        private IdxToEdtConversionService _idxToEdtConversionService;
        private NativeFileFinder nativeFileFinder;

        [OneTimeSetUp]
        public void SetIdxSample()
        {
            _idxSample = new IdxDocumentsRepository().GetSample();

            _idxDocumentIds = _idxSample.Select(x => x.DocumentId)?.ToList();

            TestSuite.Log(AventStack.ExtentReports.Status.Info, $"{_idxSample.Count()} sampled from Idx records.");
        }

        [Test]
        [TestCaseSource(typeof(IdxFieldByEdtFieldComparison), nameof(StandardMappingsToTest))]
        public void ValidateFieldPopulation(StandardMapping mappingUnderTest)
        {
            EdtFieldUnderTest = mappingUnderTest.EdtName;
            var populated = 0;
            var different = 0;
            var documentsInIdxButNotInEdt = 0;
            var documentsInEdtButNotInIdx = 0;
            var idxNoValue = 0;
            var unexpectedErrors = 0;
            var matched = 0;
            var totalSampled = 0;
            var emptyField = false;

            bool isFieldAutoPopulatedIfNull = Settings.AutoPopulatedNullFields.Contains(mappingUnderTest.EdtName);
            //initiliase conversion service for field under test
            try
            {
                _idxToEdtConversionService = new IdxToEdtConversionService(mappingUnderTest);

                using (var loadFileWriter = new LoadFileWriter())
                {
                    loadFileWriter.OutputHeader(mappingUnderTest.EdtName);

                    Test.Debug(
                        $"Using EDT database column for comparison: {_idxToEdtConversionService.MappedEdtDatabaseColumn}");

                    //Get 
                    var edtValues = GetEdtFieldValues(mappingUnderTest);

                    // if all values empty
                    if (edtValues.Values.All(string.IsNullOrWhiteSpace) && _idxSample.AsParallel()
                            .All(x => string.IsNullOrWhiteSpace(GetIdxFieldValue(x, mappingUnderTest))))
                    {
                        idxNoValue = _idxDocumentIds.Count;
                        matched = idxNoValue;
                        _idxSample.ToList().ForEach(x => loadFileWriter.OutputRecord(x.DocumentId, string.Empty));
                    }
                    else
                    {
                        //loop thru each sample document
                        _idxSample
                            .Batch(100)
                            .AsParallel()
                            .ForEach(documentBatch =>
                                documentBatch.ForEach(idxDocument =>
                                {
                                    totalSampled++;

                                    var idxField = GetIdxFieldValue(idxDocument, mappingUnderTest);

                                    var expectedEdtValue = string.IsNullOrWhiteSpace(idxField)
                                        ? idxField
                                        : _idxToEdtConversionService.ConvertValueToEdtForm(idxField);

                                    loadFileWriter.OutputRecord(idxDocument.DocumentId,
                                        idxField == null ? string.Empty : expectedEdtValue);

                                    //if edt doesnt value not obtained but idx has value
                                    if (!edtValues.TryGetValue(idxDocument.DocumentId, out var edtValueForIdxRecord) &&
                                        !string.IsNullOrEmpty(idxField))
                                    {
                                        documentsInIdxButNotInEdt++;
                                        AddComparisonError(idxDocument.DocumentId,
                                            "Document was not found in Edt, when Idx had a value");

                            }
                            else
                            {
                                if (string.IsNullOrWhiteSpace(idxField))
                                {
                                    if (!string.IsNullOrWhiteSpace(edtValueForIdxRecord))
                                    {
                                        documentsInEdtButNotInIdx++;
                                        ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxDocument.DocumentId, $"Edt had value \"{edtValueForIdxRecord}\" for field {mappingUnderTest.EdtName} when Idx had no value."));
                                    }

                                    //if idx field is null or empty
                                    if (string.IsNullOrWhiteSpace(idxField))
                                    {
                                        if (string.IsNullOrWhiteSpace(edtValueForIdxRecord))
                                        {
                                            idxNoValue++;
                                        }
                                        else
                                        {
                                            documentsInEdtButNotInIdx++;
                                            AddComparisonError(idxDocument.DocumentId,
                                                $"Edt had value \"{edtValueForIdxRecord}\" for field {mappingUnderTest.EdtName} when Idx had no value.");
                                        }

                                        return;
                                    }

                                    //else compare values found
                                    try
                                    {
                                        if (!string.IsNullOrWhiteSpace(edtValueForIdxRecord)) populated++;

                                        var trimmedActualEdtValue = edtValueForIdxRecord;//.Replace(" ", "");
                                        var trimmedExpectedEdtValue = expectedEdtValue;//.Replace(" ", "");

                                        if (!trimmedActualEdtValue.Equals(trimmedExpectedEdtValue, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            if (mappingUnderTest.EdtName.Equals("Host Document Id",
                                                StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                //if .pst, check that null
                                                var fileType = idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals("FILETYPE_PARAMETRIC", StringComparison.InvariantCultureIgnoreCase));
                                                if(fileType.Value.Equals(".pst", StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    matched++;
                                                    ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxDocument.DocumentId, "Host document id null due to being .pst"));
                                                }  
                                                else
                                                {
                                                    different++;
                                                    ComparisonResults.Add(
                                                        new Framework.Models.Reporting.ComparisonResult(
                                                            idxDocument.DocumentId, edtValueForIdxRecord,
                                                            expectedEdtValue, idxField));
                                                }
                                            }
                                            
                                            different++;
                                            ComparisonResults.Add(new Framework.Models.Reporting.ComparisonResult(idxDocument.DocumentId, edtValueForIdxRecord, expectedEdtValue, idxField));
                                        }
                                        else
                                        {
                                            matched++;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        unexpectedErrors++;
                                        var error = $"{ex.Message}<br></br>{ex.StackTrace}";
                                        ComparisonErrors.Add(
                                            new Framework.Models.Reporting.ComparisonError(idxDocument.DocumentId,
                                                error));
                                    }
                                }));
                    }

                    PrintStats(different, matched, documentsInIdxButNotInEdt, documentsInEdtButNotInIdx, idxNoValue,
                        unexpectedErrors, populated, totalSampled);


                    Assert.Zero(different,
                        $"Differences were seen between expected value and actual value for this Edt field {mappingUnderTest.EdtName}");
                    Assert.Zero(unexpectedErrors,
                        $"Unexpected errors experienced during processing {mappingUnderTest.EdtName}");
                    Assert.Zero(documentsInIdxButNotInEdt, $"Idx documents were not found in EDT.");

                    Assert.Zero(documentsInEdtButNotInIdx,
                        "Edt was found to have field populated for instances where Idx was null");

                    if (idxNoValue > 0)
                        Test.Info(
                            $"The Idx was found to not have a value for field {mappingUnderTest.IdxNames} in {idxNoValue} documents/instances.");

                    if (populated == 0)
                        Test.Info($"No sampled documents had the Edt field {mappingUnderTest.EdtName} populated.");
                }
            }
            catch (EdtColumnException e)
            {
                if (DoesIdxContainAnyValue(mappingUnderTest))
                {
                    Assert.Fail($"Idx has value but Edt column not found {e.Message}");
                }
                else
                {
                    UnMappedFieldLogger.Instance.WriteUnmappedFile(string.Join(";", mappingUnderTest.IdxNames),
                        mappingUnderTest.EdtName);
                }
            }
        }

        private string GetIdxFieldValue(Framework.Models.IdxLoadFile.Document idxDocument,
            StandardMapping mappingUnderTest)
        {
            var allValues = new List<string>();

            foreach (var idxName in mappingUnderTest.IdxNames)
            {
                var idxNameLookup = idxName.StartsWith("#DRE") ? idxName.Substring(4) : idxName;

                /*if (mappingUnderTest.EdtType != "MultiValueList" && !mappingUnderTest.IsEmailField())
                    return idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(idxName))?.Value;*/

                var allFieldValues = idxDocument.AllFields.Where(x =>
                        x?.Key != null && x.Value != null && x.Key.Equals(idxNameLookup) &&
                        !string.IsNullOrWhiteSpace(x.Value))
                    .Select(x => x.Value)
                    .Distinct()
                    .OrderBy(x => x);

                allValues.AddRange(allFieldValues);
            }

            var delimiter = (mappingUnderTest.EdtType.ToLower().Contains("list") || mappingUnderTest.IsEmailField())
                ? ";"
                : "; ";

            return string.Join(delimiter, allValues);
        }

        private bool DoesIdxContainAnyValue(StandardMapping mappingUnderTest)
        {
            var hasIdxValue = false;

            foreach (var idxName in mappingUnderTest.IdxNames)
            {
                var idxNameLookup = idxName.StartsWith("#DRE") ? idxName.Substring(4) : idxName;

                var hasValue = _idxSample.Any(x => x.AllFields.Any(y => y.Key.Equals(idxNameLookup)));

                if (hasValue)
                    hasIdxValue = true;
            }

            return hasIdxValue;
        }

        private Dictionary<string, string> GetEdtFieldValues(StandardMapping mappingUnderTest)
        {
            if (mappingUnderTest.IsEmailField())
            {
                return GetEmailFieldValues(_idxDocumentIds, mappingUnderTest.EdtName);
            }

            if ((!string.IsNullOrEmpty(mappingUnderTest.EdtType) &&
                 mappingUnderTest.EdtType.Equals("MultiValueList", StringComparison.InvariantCultureIgnoreCase)) ||
                (_idxToEdtConversionService.MappedEdtDatabaseColumnType.HasValue &&
                 _idxToEdtConversionService.MappedEdtDatabaseColumnType.Value == ColumnType.MultiValueList))
            {
                var allFieldValues = EdtDocumentRepository.GetMultiValueFieldValues(_idxDocumentIds,
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
                    ? EdtDocumentRepository.GetDocumentDateField(_idxDocumentIds,
                        _idxToEdtConversionService.MappedEdtDatabaseColumn)
                    : EdtDocumentRepository.GetDocumentField(_idxDocumentIds,
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

        private void PrintStats(long different, long matched, long documentsInIdxButNotInEdt,
            long documentsInEdtButNotIdx, long idxMissingField, long unexpectedErrors, long populated, long total)
        {
            LogComparisonStatistics(new string[][]
            {
                new string[] {"Differences", different.ToString()},
                new string[] {"Matched", matched.ToString()},
                new string[]
                    {"Idx document(s) incorrectly without a value in Edt", documentsInIdxButNotInEdt.ToString()},
                new string[]
                    {"Edt document(s) incorrectly have a value when Idx is null", documentsInEdtButNotIdx.ToString()},
                //new string[] { "Idx document(s) not populated for field under test (and EDt is also null)", idxMissingField.ToString() },
                new string[] {"Unexpected Errors during processing", unexpectedErrors.ToString()}
            });

            LogComparisonStatistics(new string[][]
            {
                new string[] {"Populated", populated.ToString()},
                new string[] {"Empty", (total - populated).ToString()},
            }, "Populate Statistics");
        }

        private static IEnumerable<TestCaseData> StandardMappingsToTest
        {
            get
            {
                return
                    new StandardMapReader()
                        .GetStandardMappings()
                        .Where(x => !string.IsNullOrEmpty(x.EdtName) &&
                                    !x.EdtName.Equals("UNMAPPED", StringComparison.InvariantCultureIgnoreCase) &&
                                    x.IdxNames.Any())
                        .Select(x => new TestCaseData(x)
                            .SetName($"\"{string.Join("|", x.IdxNames)}\" vs \"{x.EdtName}\"")
                            .SetDescription(
                                $"For subset of data compare Edt database field values for \"{string.Join("|", x.IdxNames)}\" with Idx values of field \"{x.EdtName}\""));
            }
        }
    }
}