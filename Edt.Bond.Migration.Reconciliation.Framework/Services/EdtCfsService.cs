using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase.Dto;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class EdtCfsService
    {
        public static long GetDocumentCountForBatch()
        {
            var allFileLocations = EdtDocumentRepository.GetNativeFileLocations().ToList();

            LogNativeFileLocations(allFileLocations);

            var presentFileLocations = allFileLocations.Where(x => DoesDocumentExistInNativeStore(x));

            return presentFileLocations.LongCount();
        }

        private static bool DoesDocumentExistInNativeStore(DerivedFileLocation derivedFileLocation)
        {
            var filePath = BuildDocFileName(derivedFileLocation);

            var exists =  File.Exists(filePath);

            return exists;
        }

        private static string BuildDocFileName(DerivedFileLocation derivedFileLocation)
        {
            var caseStoreLocation = Path.Combine(Settings.EdtCfsDirectory, $"Site01_Case{Settings.EdtCaseId.PadLeft(4, '0')}\\Docs");

            var filePath = Path.Combine(caseStoreLocation, $"{derivedFileLocation.FolderId}\\{derivedFileLocation.DocumentId}\\{derivedFileLocation.Filename}");

            return filePath;
        }

        private static void LogNativeFileLocations(List<DerivedFileLocation> derivedFiles)
        {
            using (var sw = new StreamWriter(".\\logs\\NativeComparison_DerivedLocations.csv"))
            {
                sw.WriteLine("DocumentId,Filename,FolderId,Full Path");

                foreach (var file in derivedFiles)
                {
                    sw.WriteLine($"{file.DocumentId},{file.Filename},{file.FolderId},{BuildDocFileName(file)}");
                }
            }
        }
    }
}
