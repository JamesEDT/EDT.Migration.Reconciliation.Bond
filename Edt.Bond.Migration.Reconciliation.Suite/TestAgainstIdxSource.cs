using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite
{
    [SetUpFixture]
    public class TestAgainstIdxSource
    {
        public static IEnumerable<StandardMapping> StandardMappings;
        public static IdxDocumentsRepository IdxDocumentsRepository;

        public static long IdxDocumentsProcessed = 0;

        [OneTimeSetUp]
        public void AnalyseIdx()
        {
            StandardMappings = new StandardMapReader().GetStandardMappings();

            if (!StandardMappings.Any())
                throw new Exception("Failed to read mappings - count is 0 post attempt");

            var idxPath = Path.Combine(Settings.MicroFocusSourceDirectory, Settings.IdxName);

            Document[] documents;

            IdxDocumentsRepository = new IdxDocumentsRepository();
            IdxDocumentsRepository.Initialise(true);

            do
            {
                documents = IdxProcessingService.GetNextDocumentChunkFromFile(idxPath).ToArray();

                if (!documents.Any()) return;

                IdxDocumentsRepository.AddDocuments(documents);
                IdxDocumentsProcessed =+ documents.Length;

            } while (documents.Length > 0);

            IdxDocumentsRepository.CreateDocumentIdIndex();
        }

    }
}
