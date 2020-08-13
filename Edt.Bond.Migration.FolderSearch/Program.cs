using Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edt.Bond.Migration.FolderSearch
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = @"C:\EDT\Batch 4\RE00979_DEN\Extract\1425\IDX\NON-LPP";

            var idxPaths = Directory.GetFiles(dir, "*.idx", SearchOption.AllDirectories);

            var wantedValue = "Dinesh Karnuadhara";

            var folderHits = 0;
            var idolField = new List<string>() {
                "TAG_SUBJECTS_MATCH",
                "TAG_CIV_SUBJECTS_MATCH"
                
            };

            using (var writer = new StreamWriter(@"C:\EDT\clayon_docId.csv"))
            {
                for (var i = 0; i < idxPaths.Length; i++)
                {

                    var idxProcessingService = new IdxReaderByChunk(File.OpenText(idxPaths[i]));
                    Console.WriteLine($"New Idx {i}");
                    List<Document> documents;
                    long sampled = 0;

                    do
                    {
                        documents = idxProcessingService.GetNextDocumentBatch()?.ToList();

                        if (documents != null)
                        {
                            documents
                                .AsParallel()
                                .ForEach(x =>
                                {
                                    if (//x.GetValuesForIdolFields(idolField).Any(v => v.Equals(folderId, StringComparison.InvariantCultureIgnoreCase))
                                           x.DocumentId.Equals(wantedValue, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        folderHits++;
                                        writer.WriteLine(x.DocumentId);
                                    }
                                });

                            sampled += documents.Count();
                            Console.Write($"\rSampled {sampled} - Found {folderHits}");
                        }
                    } while (!idxProcessingService.EndOfFile);

                   
                }
            }

            Console.ReadLine();
        }
    }
}
