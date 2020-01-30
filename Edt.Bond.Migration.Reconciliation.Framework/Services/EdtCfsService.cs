using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase.Dto;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class EdtCfsService
    {

        public static IEnumerable<string> GetDocumentsForBatch()
        {
            var allDocumentIds = EdtDocumentRepository.GetAllDocumentIds();

            var allNativeFilesInEdt = Directory
                .GetFiles(Path.Combine(Settings.EdtCfsDirectory, $"Site01_Case{Settings.EdtCaseId.ToString().PadLeft(4, '0')}"), "*.*", SearchOption.AllDirectories)
                .Select(x => new FileInfo(x).Name)
                .Select(x => x.Substring(0, x.IndexOf('_')))
                .Distinct()
                .ToList();

            var presentDocuments = allDocumentIds.Where(x => allNativeFilesInEdt.Contains( x.DocumentId.ToString()));

            return presentDocuments.ToList().Select(x => (string) x.DocumentNumber);
        }

        private static void LogNativeFileLocations(List<DerivedFileLocation> derivedFiles)
        {
            using (var sw = new StreamWriter(Path.Combine(Settings.LogDirectory, "NativeComparison_DerivedLocations.csv")))
            {
                sw.WriteLine("DocumentId,Filename,FolderId,Full Path");

                foreach (var file in derivedFiles)
                {
                    sw.WriteLine($"{file.DocumentId},{file.Filename},{file.FolderId},{file.FullDocumentPath}");
                }
            }
        }
    }
}
