using AventStack.ExtentReports.MarkupUtils;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using NUnit.Framework;
using System;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
    [Description("Comparing high level counts of documents from the source Idx to targets (Edt database and File store)")]
    public class HighLevelReconciliation: TestBase
    {
        private long _idxDocumentCount;

        [OneTimeSetUp]
        public void GetIdxCount()
        {
            _idxDocumentCount = new IdxDocumentsRepository().GetNumberOfDocuments();
        }

        [Test]
        [Description("Comparing the count of documents detailed in the Idx compared to the document count found in the Edt database")]
        public void DatabaseDocumentsCountsAreEqualBetweenIdxAndEdtDatabase()
        {       
            var EdtDocumentCount = EdtDocumentRepository.GetDocumentCount();

            string[][] data = new string[][]{
                new string[]{ "Item Evaluated", "Count of Documents"},
                new string[] { "Idx file", _idxDocumentCount.ToString() },
                new string[] { "Edt Database", EdtDocumentCount.ToString() }                
            };

            TestLogger.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data));

            Assert.AreEqual(_idxDocumentCount, EdtDocumentCount, "File counts should be equal for Idx and Load file");
        }

        [Test]        
        [Description("Comparing the count of documents detailed in the Idx to the Edt Central File Store, thus validating all natives are imported to EDT.")]
        public void NativeCountsAreEqualBetweenIdxAndEdtFileStore()
        {
            var cfsCount= EdtCfsService.GetDocumentCountForBatch();

            string[][] data = new string[][]{
                new string[]{ "Item Evaluated", "Count of Documents"},
                new string[] { "Idx file", _idxDocumentCount.ToString() },
                new string[] { "Edt Central file store", cfsCount.ToString() }
            };

            TestLogger.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data));

            Assert.AreEqual(_idxDocumentCount, cfsCount, "File counts should be equal for Idx and Load file");
        }

        [Test]
        [Description("Comparing the text document counts in the Idx to the Edt Central File Store, thus validating all text fies are imported to EDT.")]
        public void TextCountsAreEqualBetweenIdxAndEdtFileStore()
        {
            
            throw new NotImplementedException();
        }

    }
}
