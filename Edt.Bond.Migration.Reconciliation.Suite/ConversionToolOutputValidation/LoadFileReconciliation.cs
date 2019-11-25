using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using NUnit.Framework;
using System;

namespace Edt.Bond.Migration.Reconciliation.Suite.ConversionToolOutputValidation
{
    [TestFixture]
    public class LoadFileReconciliation
    {
        [OneTimeSetUp]
        public void ParseConversionToolLoadFile()
        {
            new ConversionToolLoadFileReader().Read();
        }

        [Test]
        public void DoesHaveExpectedDocumentCount()
        {
            var idxDocumentCount =  new IdxDocumentsRepository().GetNumberOfDocuments();

            TestContext.Out.WriteLine($"Idx document count: {idxDocumentCount}");

            var loadFileDocumentCount = new ConversionLoadFileRepository().GetNumberOfDocuments();

            TestContext.Out.WriteLine($"Conversion Tool Load File document count: {loadFileDocumentCount}");

            Assert.AreEqual(idxDocumentCount, loadFileDocumentCount, "File counts should be equal for Idx and Load file");
        }

        [Test]
        public void DoesHaveExpectedColumnCount()
        {
            var idxCount = new IdxDocumentsRepository().GetColumnSizeOfDocuments();

            TestContext.Out.WriteLine($"Idx column total: {idxCount}");

            var loadFileColumnCount = new ConversionLoadFileRepository().GetColumnSizeOfDocuments();

            TestContext.Out.WriteLine($"Conversion Tool Load File column count: {loadFileColumnCount}");

            Assert.AreEqual(idxCount, loadFileColumnCount, "File counts should be equal for Idx and Load file");
        }

        [Test]
        public void AreAllCoreColumnsPopulated()
        {
            // Sample 1000 documents
            // compare each field?
            throw new NotImplementedException();
        }
    }

}
