using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using Edt.Bond.Migration.Reconciliation.Framework.Output;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using System;
using System.Linq;

namespace Edt.Bond.Migration.LocationBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var _idxSample = new IdxDocumentsRepository().GetSample();

                var _idxDocumentIds = _idxSample.Select(x => x.DocumentId).ToList();

                using (var locationFileWriter = new LocationFileWriter())
                {
                    foreach (var idxDocument in _idxSample)
                    {
                        var emsFolder = new EmsFolder()
                        {
                            Group = idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(Settings.LocationIdxFields[0])).Value,
                            Custodian = idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(Settings.LocationIdxFields[1])).Value,
                            Source = idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(Settings.LocationIdxFields[2])).Value
                        };

                        idxDocument.AllFields.Where(c => c.Key.StartsWith(Settings.LocationIdxFields[3])).OrderBy(c => c.Key).ToList().ForEach(
                            c =>
                            {
                                if (!string.IsNullOrWhiteSpace(c.Value) && !c.Value.Contains(".msg:"))
                                {
                                    emsFolder.VIRTUAL_PATH_SEGMENTs.Add(c.Value.Replace(":", "-"));
                                }
                            });

                        locationFileWriter.OutputRecord(idxDocument.DocumentId, emsFolder.ConvertedEdtLocation);

                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
