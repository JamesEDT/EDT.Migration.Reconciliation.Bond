using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using NUnit.Framework;
using System;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
    public class HighLevelReconciliation
    {
        private long _idxDocumentCount;

        [OneTimeSetUp]
        public void GetIdxCount()
        {
            _idxDocumentCount = new IdxDocumentsRepository().GetNumberOfDocuments();
        }

        [Test]
        public void DocumentsCountsAreEqual()
        {
            TestContext.Out.WriteLine($"Idx document count: {_idxDocumentCount}");

            var EdtDocumentCount = EdtDocumentRepository.GetDocumentCount();

            TestContext.Out.WriteLine($"Edt document count for configured dataset name: {EdtDocumentCount}");

            Assert.AreEqual(_idxDocumentCount, EdtDocumentCount, "File counts should be equal for Idx and Load file");
        }

        [Test]
        [Description("Comparing something to something")]
        public void NativeCountsAreEqual()
        {
            TestContext.Out.WriteLine($"Idx document count: {_idxDocumentCount}");

            var cfsCount= EdtCfsService.GetDocumentCountForBatch();

            TestContext.Out.WriteLine($"Edt Cfs native document count for configured dataset name: {cfsCount}");

            Assert.AreEqual(_idxDocumentCount, cfsCount, "File counts should be equal for Idx and Load file");
        }

        [Test]
        public void TextCounts()
        {
            throw new NotImplementedException();
        }

    }
}
