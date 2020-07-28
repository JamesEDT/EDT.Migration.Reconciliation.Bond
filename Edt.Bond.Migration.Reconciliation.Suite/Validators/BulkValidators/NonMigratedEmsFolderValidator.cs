using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using Edt.Bond.Migration.Reconciliation.Framework.Services;

namespace Edt.Bond.Migration.Reconciliation.Suite.Validators
{
    public class NonMigratedEmsFolderValidator : BulkValidator, IDisposable
    {
        private List<Tag> workbookRecords;
        private List<string> workbookIds;

        public NonMigratedEmsFolderValidator() : base("Non Migrated EMS folders", "AUN Workbook Records")
        {
            workbookRecords = AunWorkbookReader.Read();
            workbookIds = workbookRecords.Select(x => x.Id).ToList();
        }

        public void Validate(List<Document> idxDocuments)
        {
            var allIdxDistinctTags = idxDocuments.SelectMany(x => x.AllFields.Where(field => field.Key.Equals("AUN_WORKBOOK_NUMERIC", StringComparison.InvariantCultureIgnoreCase)))
                .Select(x => x.Value)
                .Distinct()
                .ToList();

            foreach (var idxTag in allIdxDistinctTags)
            {
                var relevantWorkbookRecord = workbookRecords.SingleOrDefault(x => x.Id == idxTag);
                if (relevantWorkbookRecord != null)
                {
                    workbookIds.Remove(idxTag);
                    workbookIds.RemoveAll(x => relevantWorkbookRecord.FullTagHierarchy.Contains(x));
                }
            }
        }

        public void Dispose()
        {
            using (var sw = new StreamWriter(Path.Combine(Settings.ReportingDirectory, "nonidx_workbookRecords.csv")))
            {
                workbookIds.ForEach(x => {
                    var workbookRecord = workbookRecords.SingleOrDefault(record => record.Id == x);

                    if(workbookRecord != null)
                        sw.WriteLine($"{workbookRecord.Id}\t{workbookRecord.Name}\t{workbookRecord.FullPath}");
                });
            }
        }
    }
}
