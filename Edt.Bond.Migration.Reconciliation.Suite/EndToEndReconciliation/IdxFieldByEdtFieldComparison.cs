using AventStack.ExtentReports.MarkupUtils;
using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
    [Description("Compare Idx field with Edt Database field to validate implementation of mapping, for a subset of records.")]

    public class IdxFieldByEdtFieldComparison : TestBase
    {
        private IEnumerable<Framework.Models.IdxLoadFile.Document> _idxSample;
        private List<ColumnDetails> _edtColumnDetails;
        private List<string> _idxDocumentIds;

        [OneTimeSetUp]
        public void SetIdxSample()
        {
            _idxSample = new IdxDocumentsRepository().GetSample();

            _idxDocumentIds = _idxSample.Select(x => x.DocumentId).ToList();
            
				//don't validate MvFields atm
            _edtColumnDetails = EdtDocumentRepository.GetColumnDetails().ToList();

            FeatureRunner.Log(AventStack.ExtentReports.Status.Info, $"{_idxSample.Count()} sampled from Idx records.");
        }

        [Test]
        [TestCaseSource(typeof(IdxFieldByEdtFieldComparison), nameof(StandardMappingsToTest))]
        public void ValidateFieldPopulation(StandardMapping mappingUnderTest)
        {
            long populated = 0;
            long different = 0;
            long documentsInIdxButNotInEdt = 0;
            long documentsInEdtButNotInIdx = 0;
            long idxUnfound = 0;
            long unexpectedErrors = 0;
            long matched = 0;
            long totalsampled = 0;


            if (mappingUnderTest.EdtType == "MultiValueList")
            {
	            return;
            }
            //Get 
            var edtValues = GetEdtFieldValues(mappingUnderTest);
            

            //loop thru each sample document
            foreach (var idxDocument in _idxSample)
            {
                totalsampled++;

                var idxField = idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(mappingUnderTest.IdxName));

                if (!edtValues.TryGetValue(idxDocument.DocumentId, out var edtValueForIdxRecord) && idxField?.Value != null)
                {
                    documentsInIdxButNotInEdt++;
                    ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxDocument.DocumentId, "Document not found in Edt's document table"));
                }
                else
                {

                    if (idxField == null)
                    {
                        if (!string.IsNullOrEmpty(edtValueForIdxRecord))
                        {
                            documentsInEdtButNotInIdx++;
                            ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxDocument.DocumentId, $"Edt had value {edtValueForIdxRecord} for field {mappingUnderTest.EdtName} when Idx had no value."));
                        }
                        else
                        {
                            idxUnfound++;
                            ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxDocument.DocumentId, $"Field { mappingUnderTest.IdxName } not found in Idx for document"));
                        }
                    }
                    else
                    {
                        try
                        {                            
                            if (!string.IsNullOrEmpty(edtValueForIdxRecord)) populated++;

                            var expectedEdtValue = IdxToEdtConversionService.ConvertValueToEdtForm(mappingUnderTest.EdtType, idxField.Value);

                            if (!edtValueForIdxRecord.Equals(expectedEdtValue, StringComparison.InvariantCultureIgnoreCase))
                            {
                                different++;
                                ComparisonResults.Add(new Framework.Models.Reporting.ComparisonResult(idxDocument.DocumentId, edtValueForIdxRecord, expectedEdtValue, idxField.Value));
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
                            ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxDocument.DocumentId, error));
                        }
                    }

                }
            }

            PrintStats(different, matched, documentsInIdxButNotInEdt, documentsInEdtButNotInIdx, idxUnfound, unexpectedErrors, populated, totalsampled);

            if (ComparisonErrors.Count() > 0 || ComparisonResults.Count() > 0)
            {
                var diffFile = PrintComparisonTables(mappingUnderTest.EdtName);
                TestLogger.Info($"Difference and error details written to: <a href=\"{diffFile}\">{diffFile}</a>");
            }

            Assert.Zero(different, $"Differences were seen between expected value and actual value for this Edt field {mappingUnderTest.EdtName}");
            Assert.Zero(unexpectedErrors, $"Unexpected errors experienced during processing {mappingUnderTest.EdtName}");
            Assert.Zero(documentsInIdxButNotInEdt, $"Idx documents were not found in EDT.");
            Assert.Zero(documentsInEdtButNotInIdx, "Edt was found to have field populated for instances where Idx was null");
           
            if (idxUnfound > 0)
                TestLogger.Info($"The Idx was found to not have a value for field {mappingUnderTest.IdxName} in {idxUnfound} documents/instances.");

            if(populated == 0)
                TestLogger.Info($"No sampled documents had the Edt field {mappingUnderTest.EdtName} populated.");          
        }

        private Dictionary<string,string> GetEdtFieldValues(StandardMapping mappingUnderTest)
        {
            if(mappingUnderTest.IsEmailField())
            {
                var correspondances = GetEmailFieldValues(_idxSample.Select(x => x.DocumentId).ToList(), mappingUnderTest.EdtName);

                return correspondances;
            }


            else
            {
                var edtDbName = GetEdtDatabaseNameFromDisplayName(mappingUnderTest.EdtName);
                TestLogger.Debug($"Using EDT database column for comparison: {edtDbName}");

                return (mappingUnderTest.EdtType == "Date") ? EdtDocumentRepository.GetDocumentDateField(_idxDocumentIds, edtDbName): EdtDocumentRepository.GetDocumentField(_idxDocumentIds, edtDbName);
               
            }
        }

        private Dictionary<string, string> GetEmailFieldValues(List<string> documentIds, string fieldType)
        {
            var type = fieldType.Replace(Settings.EmailFieldIdentifyingPrefix, string.Empty);

            var allFields = EdtDocumentRepository.GetDocumentCorrespondances(documentIds)
                            .Where(x => x.CorrespondanceType.Equals(type, StringComparison.InvariantCultureIgnoreCase));

            var desiredParties = from field in allFields
                                 group field.PartyName by field.DocumentNumber into correspondants
                                 select new { DocumentId = correspondants.Key, Value = string.Join(",", correspondants.ToList())};


            return desiredParties.ToDictionary(x => (string) x.DocumentId, x => x.Value);
        }


        private string GetEdtDatabaseNameFromDisplayName(string displayName)
        {
            var lowerDisplayName = displayName.ToLower();

            var matchedDbName = _edtColumnDetails.FirstOrDefault(x => x.DisplayName.ToLower().Equals(lowerDisplayName));

            if(matchedDbName == null)
            {
                Regex rgx = new Regex("[^a-zA-Z0-9]");
                lowerDisplayName = rgx.Replace(lowerDisplayName, "");
                
                matchedDbName = _edtColumnDetails.Find(x => x.GetAlphaNumbericOnlyDisplayName().ToLower()
                                            .Replace(" ", string.Empty).Equals(lowerDisplayName));

                return matchedDbName != null ? matchedDbName.ColumnName : throw new Exception($"Unable to determine Edt Db column name from mapped display name {displayName}");
            }
            else
            {
                return matchedDbName.ColumnName;
            }
        }

        private void PrintStats(long different, long matched, long documentsInIdxButNotInEdt, long documentsInEdtButNotIdx, long idxMissingField, long unexpectedErrors, long populated, long total)
        {
            string[][] data = new string[][]{
                new string[]{ "<b>Comparison Statistics:</b>"},
                new string[]{ "Statistic", "Count"},
                new string[] { "Differences", different.ToString() },
                new string[] { "Matched", matched.ToString() },
                new string[] { "Idx document(s) incorrectly without a value in Edt", documentsInIdxButNotInEdt.ToString() },
                new string[] { "Edt document(s) incorrectly have a value when Idx is null", documentsInEdtButNotIdx.ToString() },
                new string[] { "Idx document(s) not populated for field under test (and EDt is also null)", idxMissingField.ToString() },
                new string[] { "Unexpected Errors during processing", unexpectedErrors.ToString()},
                new string[] { "Edt documents(s) populated with a value", populated.ToString()},
                new string[] { "Total Idx records sampled", total.ToString()}
            };

            TestLogger.Info(MarkupHelper.CreateTable(data));
        }
        
        private static IEnumerable<TestCaseData> StandardMappingsToTest {
            get
            {
                return
                    new StandardMapReader()
                    .GetStandardMappings()
                    .Where(x => !string.IsNullOrEmpty(x.EdtName) && !string.IsNullOrEmpty(x.IdxName))
                    .Select(x => new TestCaseData(x)
                        .SetName($"\"{x.IdxName}\" vs \"{x.EdtName}\"")
                        .SetDescription($"For subset of data compare Edt database field values for \"{x.IdxName}\" with Idx values of field \"{x.EdtName}\""));
            }
        }
    }
}
