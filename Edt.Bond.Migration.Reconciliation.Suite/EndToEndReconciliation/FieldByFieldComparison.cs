using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
    public class FieldByFieldComparison
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
        }

        [Test]
        [TestCaseSource(typeof(FieldByFieldComparison), nameof(StandardMappingsToTest))]
        public void ValidateFieldPopulation(StandardMapping mappingUnderTest)
        {
            long populated = 0;
            long different = 0;
            long orphanDocuments = 0;
            long idxUnfound = 0;
            long edtUnfound = 0;
            long matched = 0;

            var edtDbName = GetEdtDatabaseNameFromDisplayName(mappingUnderTest.EdtName);            

            //loop thru each sample document
            foreach (var idxDocument in _idxSample)
            {
                var matchedEdtDocument = _edtDocuments.FirstOrDefault(x => x["DocNumber"].ToString().Equals(idxDocument.DocumentId));
                if(matchedEdtDocument == null)
                {
                    orphanDocuments++;
                    TestContext.Out.WriteLine($"Idx Document not found in Edt's document table (DocNumber: {idxDocument.DocumentId})");
                }
                else
                {
                    var idxField = idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(mappingUnderTest.IdxName));

                    if (idxField == null)
                    {
                        idxUnfound++;
                        TestContext.Out.WriteLine($"Idx field {mappingUnderTest.IdxName} not found in Idx (DocNumber: {idxDocument.DocumentId})");
                    }
                    else
                    {
                        try
                        {
                            var edtValue = matchedEdtDocument[edtDbName];

                            if (!string.IsNullOrEmpty(edtValue.ToString())) populated++;

                            var expectedEdtValue = IdxToEdtConversionService.ConvertValueToEdtForm(mappingUnderTest.EdtType, idxField.Value);

                            if(!edtValue.ToString().Equals(expectedEdtValue, StringComparison.InvariantCultureIgnoreCase))
                            {
                                different++;
                                TestContext.Out.WriteLine($"***Difference Seen for :{idxDocument.DocumentId.ToString()}***");
                                TestContext.Out.WriteLine($"Edt value:{edtValue.ToString()}");
                                TestContext.Out.WriteLine($"Converted Idx value:{expectedEdtValue.ToString()}");
                            }
                            else
                            {
                                matched++;
                            }
                        }
                        catch (Exception)
                        {
                            edtUnfound++;
                            TestContext.Out.WriteLine($"Edt version of document was not found to have expected column {mappingUnderTest.EdtName}");
                        }
                    }
                }
            }

            TestContext.Out.WriteLine("**Statistics:**");
            TestContext.Out.WriteLine("Differences:"+ different);
            TestContext.Out.WriteLine("Matched:" + matched);
            TestContext.Out.WriteLine("Orphans:" + orphanDocuments);
            TestContext.Out.WriteLine("Number of missing idx field values:" + idxUnfound);
            TestContext.Out.WriteLine("No.of missing Edt field values:" + edtUnfound);
            TestContext.Out.WriteLine("No.of populated Edt field values:" + populated);

            Assert.Positive(populated, $"No samples had the Edt field {mappingUnderTest.EdtName} populated.");
            Assert.Zero(orphanDocuments, $"Orphan documents were found (difference count: {different})");
            Assert.Zero(different, $"Differences were seen between expected value and actual value for this Edt field {mappingUnderTest.EdtName} (difference count: {different})");
            Assert.Zero(idxUnfound, $"Field values were missing for this field in the Idx: {mappingUnderTest.IdxName} (missing count: {idxUnfound})");
            Assert.Zero(edtUnfound, $"Field values were missing for this field in Edt: {mappingUnderTest.EdtName} (missing count: {edtUnfound})");
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
        private static IEnumerable<TestCaseData> StandardMappingsToTest {
            get
            {
                return
                    new StandardMapReader()
                    .GetStandardMappings()
                    .Select(x => new TestCaseData(x)
                        .SetName($"Field Value Sampling: {x.IdxName} to {x.EdtName}")
                        .SetDescription("For subset of data compare Edt database value with Idx value"));
            }
        }
    }
}
