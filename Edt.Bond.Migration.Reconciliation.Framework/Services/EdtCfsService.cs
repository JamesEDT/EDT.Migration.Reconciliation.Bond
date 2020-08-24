using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase.Dto;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using System;
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

            var allNativeFilesInEdt = GetFileList();            

            var presentDocuments = allDocumentIds.Where(x => allNativeFilesInEdt.Contains( x.DocumentId.ToString()));

            return presentDocuments.ToList().Select(x => (string) x.DocumentNumber);
        }

        private static List<string> GetFileList()
        {
            if (File.Exists(Path.Combine(Settings.LogDirectory, $"{Settings.EdtCaseId}_cfs.txt")))
            {
                using(var reader = new StreamReader(Path.Combine(Settings.LogDirectory, $"{Settings.EdtCaseId}_cfs.txt")))
                {
                    var files = new List<string>();

                    while (!reader.EndOfStream)
                        files.Add(reader.ReadLine());

                    return files;
                }
            }
            else
            {
                var allNativeFilesInEdt = Directory
                    .GetFiles(Path.Combine(Settings.EdtCfsDirectory, $"Site01_Case{Settings.EdtCaseId.ToString().PadLeft(4, '0')}\\Docs"), "*.*", SearchOption.AllDirectories)
                    .Select(x => Path.GetFileName(x))
                    .Select(x => x.Substring(0, x.IndexOf('_')))
                    .Distinct()
                    .ToList();

                using (var writer = new StreamWriter(Path.Combine(Settings.LogDirectory, $"{Settings.EdtCaseId}_cfs.txt")))
                {
                    allNativeFilesInEdt.ForEach(x => writer.WriteLine(x));
                }

                return allNativeFilesInEdt;
            }
        }

        public static string GetCaseSize()
        {
            var totalBytes = 
                Directory
                .GetFiles(Path.Combine(Settings.EdtCfsDirectory, $"Site01_Case{Settings.EdtCaseId.ToString().PadLeft(4, '0')}"), "*.*", SearchOption.AllDirectories)
                .Sum( x => new FileInfo(x).Length);

            var inMb = ((decimal) totalBytes / 1024) / 1024;

            return $"{Math.Round(inMb, 1)} Mb";
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
