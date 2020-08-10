using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.IdxSearch
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter Idx path:");

            var idxPath = Console.ReadLine();

            while (true)
            {
                Console.WriteLine();
                Console.Write("Enter DocNumber:");

                var docNumber = Console.ReadLine().Trim();

                var idxProcessingService = new IdxReaderByChunk(File.OpenText(idxPath.Replace("\"", string.Empty)));

                List<Document> documents;
                bool found = false;
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
                                if (x.DocumentId.Equals(docNumber, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    found = true;
                                    x.AllFields.ToList().ForEach(f =>
                                    {
                                        Console.WriteLine($"{f.Key} = {f.Value}");
                                    });
                                }
                            });

                        sampled += documents.Count();
                        Console.Write($"\rSampled {sampled}");


                    }
                } while ((documents?.Count > 0 || !idxProcessingService.EndOfFile) && !found);

                if (!found)
                {
                    Console.WriteLine("Not Found");
                }
            }
        }
    }
}
