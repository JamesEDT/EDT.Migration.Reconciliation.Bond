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

            var presentFileLocations = allFileLocations.Where(x => File.Exists(x.FullDocumentPath));

            return presentFileLocations.LongCount();
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
