using AventStack.ExtentReports.MarkupUtils;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
    [Description("Compare Idx field with Edt Database field to validate implementation of mapping, for a subset of records.")]

    public class IdxFieldByEdtFieldComparison : TestBase
    {
        private IEnumerable<Framework.Models.IdxLoadFile.Document> _idxSample;
        private IEnumerable<IDictionary<string, object>> _edtDocuments;
        private List<ColumnDetails> _edtColumnDetails;

        [OneTimeSetUp]
        public void SetIdxSample()
        {
            _idxSample = new IdxDocumentsRepository().GetSample();

            var ids = _idxSample.Select(x => x.DocumentId).ToList();

            _edtDocuments = EdtDocumentRepository.GetDocuments(ids).ToList();

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

            var edtDbName = GetEdtDatabaseNameFromDisplayName(mappingUnderTest.EdtName);            

            //loop thru each sample document
            foreach (var idxDocument in _idxSample)
            {
                totalsampled++;

                var matchedEdtDocument = _edtDocuments.FirstOrDefault(x => x["DocNumber"].ToString().Equals(idxDocument.DocumentId));
                if(matchedEdtDocument == null)
                {
                    orphanDocuments++;
                    ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxDocument.DocumentId, "Idx Document not found in Edt's document table"));
                }
                else
                {
                    var idxField = idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(mappingUnderTest.IdxName));

                    if (idxField == null)
                    {
                        idxUnfound++;
                        ComparisonErrors.Add(new Framework.Models.Reporting.ComparisonError(idxDocument.DocumentId, $"Idx field { mappingUnderTest.IdxName } not found in Idx"));
                    }
                    else
                    {
                        try
                        {
                            var edtValue = matchedEdtDocument[edtDbName];

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

            Assert.Positive(populated, $"No samples had the Edt field {mappingUnderTest.EdtName} populated.");
            Assert.Zero(orphanDocuments, $"Idx documents were not found in EDT (count: {different})");
            Assert.Zero(different, $"Differences were seen between expected value and actual value for this Edt field {mappingUnderTest.EdtName} (difference count: {different})");                   
            Assert.Zero(edtUnfound, $"Field values were missing for this field in Edt when Idx had a value: {mappingUnderTest.EdtName} (missing count: {edtUnfound})");

            if (idxUnfound > 0)
                TestLogger.Info($"Field values were missing for this field in the Idx: {mappingUnderTest.IdxName} (count: {idxUnfound})");
        }


        private string GetEdtDatabaseNameFromDisplayName(string displayName)
        {
            var lowerDisplayName = displayName.ToLower();

            var matchedDbName = _edtColumnDetails.FirstOrDefault(x => x.DisplayName.ToLower().Equals(lowerDisplayName));

            if(matchedDbName == null)
            {
                lowerDisplayName = lowerDisplayName.Replace(" ", string.Empty);

                matchedDbName = _edtColumnDetails.Find(x => x.DisplayName.ToLower().Replace(" ", string.Empty).Equals(lowerDisplayName));

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
