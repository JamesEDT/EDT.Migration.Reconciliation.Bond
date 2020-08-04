using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.Validators
{
    public class ServeredParentsFieldValidator : BulkValidator, IDisposable
    {
        private ConcurrentBag<string> ParentPsts = new ConcurrentBag<string>();
        private ConcurrentDictionary<string, string> SubChildParentLinks = new ConcurrentDictionary<string, string>();
        private ConcurrentBag<string> ParentZips= new ConcurrentBag<string>();

        public ServeredParentsFieldValidator() : base("Migrared Parent PST/ZIP ID", "DREPARENTREFERENCE_ID")
        {
        }

        public void Validate(List<Document> documents)
        {
            //var allEdtTags = EdtDocumentRepository.GetDocumentTags(documents.Select(x => x.DocumentId).ToList());

            //extract parents

            //check if popuated            

            documents.ForEach(idxRecord =>
            {
                var hostId = idxRecord.GetValuesForIdolFields(new List<string> { "DREPARENTREFERENCE_ID" }).FirstOrDefault();
                var childCount = idxRecord.GetValuesForIdolFields(new List<string> { "DRECHILDCOUNT" }).FirstOrDefault();
                var extension = idxRecord.GetFileExtension()?.ToLower();

                if(string.IsNullOrWhiteSpace(hostId) && !string.IsNullOrWhiteSpace(childCount) && childCount != "0")
                {
                    //then parent 
                    if(extension.EndsWith("zip"))
                    {
                        ParentZips.Add(idxRecord.DocumentId);
                    }
                    else if(extension.EndsWith("pst"))
                    {
                        ParentPsts.Add(idxRecord.DocumentId);
                    }
                }
                else
                {
                    //child of parent that has children
                    if(!string.IsNullOrWhiteSpace(childCount) && childCount != "0")
                    {
                        if (!string.IsNullOrWhiteSpace(hostId) && ParentPsts.Contains(hostId) || ParentZips.Contains(hostId) || SubChildParentLinks.ContainsKey(hostId))
                        {
                            SubChildParentLinks.TryAdd(idxRecord.DocumentId, hostId);
                        }
                    }
                }
            });
        }

        public void Dispose()
        {
           
        }
    }
}
