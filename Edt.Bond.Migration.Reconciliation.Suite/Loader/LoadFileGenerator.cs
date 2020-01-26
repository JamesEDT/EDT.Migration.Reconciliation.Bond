using AventStack.ExtentReports.MarkupUtils;
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
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
    [Category("LoadFile")]

    public class LoadFileGenerator : TestBase
    {
        private IEnumerable<Framework.Models.IdxLoadFile.Document> _idxSample;
        private List<string> _idxDocumentIds;
        private IdxToEdtConversionService _idxToEdtConversionService;


        [Test]    
        public void Generate()
        {
            _idxSample = new IdxDocumentsRepository().GetSample();

            var standardMappings = new StandardMapReader().GetStandardMappings().Where(x => !string.IsNullOrEmpty(x.EdtName) && x.IdxNames.Any(y => !string.IsNullOrWhiteSpace(y))).ToList();

            var conversionServices = standardMappings.Select(x => new IdxToEdtConversionService(x));


            //initiliase conversion service for field under test
            try
            {

                using (var loadFileWriter = new FullLoadFileWriter())
                {
                    loadFileWriter.OutputHeaders(standardMappings.Select(x => x.EdtName).ToList());

                    foreach(var idxrecord in _idxSample)
                    {
                       
                        var values = standardMappings.Select(x =>
                        {
                            var converter = conversionServices.First(y => y._standardMapping.EdtName == x.EdtName);

                            return converter.ConvertValueToEdtForm(idxrecord.AllFields.FirstOrDefault(f => f.Key == x.IdxNames.First())?.Value ?? idxrecord.AllFields.FirstOrDefault(f => f.Key == x.IdxNames.Last())?.Value);
                        });

                        loadFileWriter.OutputRecord(idxrecord.DocumentId, values);
                    }
                }
            }
            catch(Exception e)
            {
                DebugLogger.Instance.WriteException(e, "load generator");
            }
        }

        private string GetIdxFieldValue(Framework.Models.IdxLoadFile.Document idxDocument, StandardMapping mappingUnderTest)
        {
            List<string> allValues = new List<string>();

            foreach (var idxName in mappingUnderTest.IdxNames)
            {
                var idxNameLookup = idxName.StartsWith("#DRE") ? idxName.Substring(4) : idxName;

                /*if (mappingUnderTest.EdtType != "MultiValueList" && !mappingUnderTest.IsEmailField())
                    return idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(idxName))?.Value;*/

                var allFieldValues = idxDocument.AllFields.Where(x => x.Key.Equals(idxNameLookup))
                                        .Select(x => x.Value)
                                        .Distinct()
                                        .OrderBy(x => x);

                allValues.AddRange(allFieldValues);
            }

            return string.Join(";", allValues);
            
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

        private Dictionary<string,string> GetEdtFieldValues(StandardMapping mappingUnderTest)
        {
            if(mappingUnderTest.IsEmailField())
            {
                return GetEmailFieldValues(_idxDocumentIds, mappingUnderTest.EdtName);                
            }            

            if ((!string.IsNullOrEmpty(mappingUnderTest.EdtType) && mappingUnderTest.EdtType.Equals("MultiValueList", StringComparison.InvariantCultureIgnoreCase)) || _idxToEdtConversionService.MappedEdtDatabaseColumnType.Value == ColumnType.MultiValueList)
            {
                var allFieldValues = EdtDocumentRepository.GetMultiValueFieldValues(_idxDocumentIds, _idxToEdtConversionService.EdtColumnDetails.DisplayName);

                var combinedValues = allFieldValues.GroupBy(x => x.DocNumber)
                                        .Select(group => new
                                        {
                                            DocNumber = group.Key,
                                            Values = group.Select(x => x.FieldValue).OrderBy(x => x)
                                        });
         
                var dictionaryValues =  combinedValues.ToDictionary(x => (string) x.DocNumber, x => string.Join(";", x.Values));
                return dictionaryValues;
            }
            else
            {
                return (_idxToEdtConversionService.MappedEdtDatabaseColumnType == ColumnType.Date) ? EdtDocumentRepository.GetDocumentDateField(_idxDocumentIds, _idxToEdtConversionService.MappedEdtDatabaseColumn)
                                                                    :   EdtDocumentRepository.GetDocumentField(_idxDocumentIds, _idxToEdtConversionService.MappedEdtDatabaseColumn);               
            }
        }

        private Dictionary<string, string> GetEmailFieldValues(List<string> documentIds, string fieldType)
        {
            var type = fieldType.Replace(Settings.EmailFieldIdentifyingPrefix, string.Empty);

            var allFields = EdtDocumentRepository.GetDocumentCorrespondances(documentIds)
                            .Where(x => x.CorrespondanceType.Equals(type, StringComparison.InvariantCultureIgnoreCase));

            var desiredParties = from field in allFields
                                 group field.PartyName by field.DocumentNumber into correspondants
                                 select new { DocumentId = correspondants.Key, Value = string.Join(";", correspondants.ToList().OrderBy(x => x))};

            return desiredParties.ToDictionary(x => (string) x.DocumentId, x => x.Value);
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
                    .Where(x => !string.IsNullOrEmpty(x.EdtName) && x.IdxNames.Any())
                    .Select(x => new TestCaseData(x)
                        .SetName($"\"{string.Join("|", x.IdxNames)}\" vs \"{x.EdtName}\"")
                        .SetDescription($"For subset of data compare Edt database field values for \"{string.Join("|", x.IdxNames)}\" with Idx values of field \"{x.EdtName}\""));
            }
        }
    }
}
