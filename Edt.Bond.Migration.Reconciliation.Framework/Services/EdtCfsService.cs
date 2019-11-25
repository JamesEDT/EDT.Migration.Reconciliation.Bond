using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase.Dto;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class EdtCfsService
    {
        public static long GetDocumentCountForBatch()
        {
            var allFileLocations = EdtDocumentRepository.GetNativeFileLocations();

            var presentFileLocations = allFileLocations.Where(x => DoesDocumentExistInNativeStore(x));

            return presentFileLocations.LongCount();
        }

        private static bool DoesDocumentExistInNativeStore(DerivedFileLocation derivedFileLocation)
        {
            var caseStoreLocation = Path.Combine(Settings.EdtCfsDirectory, $"Site01_Case{Settings.EdtCaseId.PadLeft(4, '0')}\\Docs");

            var filePath = Path.Combine(caseStoreLocation, $"{derivedFileLocation.FolderId}\\{derivedFileLocation.DocumentId}\\{derivedFileLocation.Filename}");

            var exists =  File.Exists(filePath);

            return exists;
        }
    }
}
