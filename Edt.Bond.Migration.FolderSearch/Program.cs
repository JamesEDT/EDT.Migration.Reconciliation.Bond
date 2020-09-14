using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.FolderSearch
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = Settings.IdxFilePath;

            var idxPaths = Directory.GetFiles(dir, "*.idx", SearchOption.AllDirectories);

            var updateField = "cc_emsdeduplicated";
            List<string> _docIds = new List<string>();
            long counter = 0;
            for (var i = 0; i < idxPaths.Length; i++)
            {

                var idxProcessingService = new IdxReaderByChunk(File.OpenText(idxPaths[i]));
                Console.WriteLine($"New Idx {i}");
                List<Document> documents;
                long updated = 0;

                do
                {
                    documents = idxProcessingService.GetNextDocumentBatch()?.ToList();

                    if (documents != null)
                    {
                        documents
                            .AsParallel()
                            .ForEach(x =>
                            {
                                var drdbame = x.GetValuesForIdolFields(new List<string>() { "DREDBNAME" }).FirstOrDefault();

                                //var aunIsDuplicated = x.GetValuesForIdolFields(new List<string>() { "AUN_IS_DEDUPLICATED_NUMERIC" }).FirstOrDefault();

                                if (drdbame.ToLowerInvariant().StartsWith("deduplicated_"))
                                {
                                    //_docIds.Add(x.DocumentId);
                                    EdtDocumentRepository.UpdateDocumentField(x.DocumentId, updateField, "1");
                                }
                            });
                        updated += documents.Count();
                        Console.WriteLine($"updated {updated}");
                    }
                } while (!idxProcessingService.EndOfFile);

                   
            }

            Console.WriteLine("Counter:" + counter);
            //_docIds.ForEach(x => Console.WriteLine(x));
            Console.WriteLine("Complete. Press key to exit");
            Console.ReadLine();
        }


        private static string GetHostReference(Document document)
        {
            var rootFamily = document.GetValuesForIdolFields(new List<string>() { "DREROOTFAMILYREFERENCE_ID" })?.FirstOrDefault();

            if ($"{document.DocumentId}_FUID".Equals(rootFamily, StringComparison.InvariantCultureIgnoreCase) || document.DocumentId.Equals(rootFamily, StringComparison.InvariantCultureIgnoreCase))
            {
                return string.Empty;
            }
            else
            {
                return document.GetValuesForIdolFields(new List<string>() { "DREPARENTREFERENCE_ID" })?.FirstOrDefault();

            }
        }
    }
}
