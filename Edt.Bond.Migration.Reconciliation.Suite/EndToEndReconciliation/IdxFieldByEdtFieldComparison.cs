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
        private IEnumerable<IDictionary<string, object>> _edtDocuments;
        private List<ColumnDetails> _edtColumnDetails;
        private List<string> _idxDocumentIds;

        [OneTimeSetUp]
        public void SetIdxSample()
        {
            _idxSample = new IdxDocumentsRepository().GetSample();

            _idxDocumentIds = _idxSample.Select(x => x.DocumentId).ToList();

           // _edtDocuments = EdtDocumentRepository.GetDocuments(IdxDocumentIds).ToList();

            _edtColumnDetails = EdtDocumentRepository.GetColumnDetails().ToList();

            FeatureRunner.Log(AventStack.ExtentReports.Status.Info, $"{_idxSample.Count()} sampled from Idx records.");
        }

        [Test]
        [TestCaseSource(typeof(IdxFieldByEdtFieldComparison), nameof(StandardMappingsToTest))]
        public void ValidateFieldPopulation(StandardMapping mappingUnderTest)
        {
            long populated = 0;
            long different = 0;
            long orphanDocuments = 0;
            long idxUnfound = 0;
            long edtUnfound = 0;
            long matched = 0;
            long totalsampled = 0;

            //Get 
            var edtValues = GetEdtFieldValues(mappingUnderTest);
            

            //loop thru each sample document
            foreach (var idxDocument in _idxSample)
            {
                totalsampled++;

                var matchedEdtDocument = _edtDocuments.FirstOrDefault(x => x["DocNumber"].ToString().Equals(idxDocument.DocumentId));

                if (matchedEdtDocument == null)
                {
                    orphanDocuments++;
                    ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxDocument.DocumentId, "Document not found in Edt's document table"));
                }
                else
                {
                    var idxField = idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(mappingUnderTest.IdxName));

                    if (idxField == null)
                    {
                        idxUnfound++;
                        ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxDocument.DocumentId, $"Field { mappingUnderTest.IdxName } not found in Idx"));
                    }
                    else
                    {
                        try
                        {
                           
                            var edtValue = mappingUnderTest.IsEmailField() ? GetEmailFieldValue(idxDocument.DocumentId, mappingUnderTest.EdtName) : matchedEdtDocument[edtDbName];

                            if (edtValue != null)
                            {
                                if (!string.IsNullOrEmpty(edtValue?.ToString())) populated++;

                                var expectedEdtValue = IdxToEdtConversionService.ConvertValueToEdtForm(mappingUnderTest.EdtType, idxField.Value);

                                if (!edtValue.ToString().Equals(expectedEdtValue, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    different++;
                                    ComparisonResults.Add(new Framework.Models.Reporting.ComparisonResult(idxDocument.DocumentId, edtValue.ToString(), expectedEdtValue.ToString(), idxField.Value));
                                }
                                else
                                {
                                    matched++;
                                }
                            }
                            else
                            {
                                edtUnfound++;
                                ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxDocument.DocumentId, $"Field { mappingUnderTest.EdtName } not found in Edt"));
                            }
                        }
                        catch (Exception ex)
                        {
                            var error = $"{ex.Message}<br></br>{ex.StackTrace}";
                            ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxDocument.DocumentId, error));
                        }
                    }

                }
            }

            PrintStats(different, matched, orphanDocuments, idxUnfound, edtUnfound, populated, totalsampled);
            
            if(ComparisonErrors.Count() > 0 || ComparisonResults.Count() > 0)
                TestLogger.Info($"Difference and error details written to: {PrintComparisonTables(mappingUnderTest.EdtName)}");

            Assert.Zero(different, $"Differences were seen between expected value and actual value for this Edt field {mappingUnderTest.EdtName}");
            Assert.Zero(edtUnfound, $"Field missing for in Edt when Idx had a value: {mappingUnderTest.EdtName}");
            Assert.Zero(orphanDocuments, $"Idx documents were not found in EDT.");           
           
            if (idxUnfound > 0)
                TestLogger.Info($"Field values were missing for this field in the Idx: {mappingUnderTest.IdxName} (count: {idxUnfound})");

            if(populated == 0)
                TestLogger.Info($"No samples had the Edt field {mappingUnderTest.EdtName} populated.");          
        }

        private Dictionary<string,string> GetEdtFieldValues(StandardMapping mappingUnderTest)
        {
            if(mappingUnderTest.IsEmailField())
            {
                var correspondances = EdtDocumentRepository.GetDocumentCorrespondances(_idxSample.Select(x => x.DocumentId).ToList());
                var objects = correspondances.GroupBy(x => x.DocumentNumber);

                return null;
            }
            else
            {
                var edtDbName = GetEdtDatabaseNameFromDisplayName(mappingUnderTest.EdtName);
                TestLogger.Debug($"Using EDT database column for comparison: {edtDbName}");

                return EdtDocumentRepository.GetDocumentField(_idxDocumentIds, edtDbName);
               
            }
        }

        private string GetEmailFieldValues(List<string> documentIds, string fieldType)
        {
            var type = fieldType.Replace(Settings.EmailFieldIdentifyingPrefix, string.Empty);

            var allFields = EdtDocumentRepository.GetDocumentCorrespondances(documentIds);

            var desiredParties = allFields
                                .Where(x => x.CorrespondanceType.Equals(type, StringComparison.InvariantCultureIgnoreCase))
                                .Select(x => x.PartyName)
                                .OrderBy(x => x);

            return string.Join(",", desiredParties);                
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

        private void PrintStats(long different, long matched, long orphanIdx, long idxMissingField, long edtUnfound, long populated, long total)
        {
            string[][] data = new string[][]{
                new string[]{ "<b>Comparison Statistics:</b>"},
                new string[]{ "Statistic", "Count"},
                new string[] { "Differences", different.ToString() },
                new string[] { "Matched", matched.ToString() },
                new string[] { "Idx document(s) not in Edt", orphanIdx.ToString() },
                new string[] { "Idx document(s) missing field under test", idxMissingField.ToString() },
                new string[] { "Edt document(s) missing field under test", edtUnfound.ToString()},
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
