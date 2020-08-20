using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Extensions;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using Edt.Bond.Migration.Reconciliation.Framework.Output;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Edt.Bond.Migration.Reconciliation.Framework.Logging;

namespace Edt.Bond.Migration.Reconciliation.Suite.Validators
{
    public class LocationValidator : BulkValidator, IDisposable
    {
        readonly List<EmsFolder> _observedEmsFolders = new List<EmsFolder>();
        private readonly string _textSegment = Settings.LocationIdxFields[3];
        private readonly LocationFileWriter _locationFileWriter;

        public LocationValidator() : base("Location", string.Join("\\",Settings.LocationIdxFields))
        {
            _locationFileWriter = new LocationFileWriter();
        }

        public void Validate(List<Document> documents)
        {
            var allEdtLocations = EdtDocumentRepository.GetDocumentLocations(documents.Select(x => x.DocumentId).ToList());

            documents.ForEach(idxDocument =>
            {
                allEdtLocations.TryGetValue(idxDocument.DocumentId, out var edtLocation);

                if (string.IsNullOrWhiteSpace(edtLocation))
                {
                    TestResult.Different++;
                    TestResult.AddComparisonError(idxDocument.DocumentId, "Location not present in EDT");
                }

                var emsFolder = new EmsFolder()
                {
                    Group = (idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(Settings.LocationIdxFields[0])) ?? idxDocument.AllFields.SingleOrDefault(x =>
                    x.Key.Equals("EMS DocLibrary Group", StringComparison.InvariantCultureIgnoreCase)))?.Value.Trim(),
                    Custodian = (idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(Settings.LocationIdxFields[1])) ?? idxDocument.AllFields.SingleOrDefault(x =>
                    x.Key.Equals("EMS DocLibrary Custodian", StringComparison.InvariantCultureIgnoreCase)))?.Value.Trim(),
                    Source = (idxDocument.AllFields.FirstOrDefault(x => x.Key.Equals(Settings.LocationIdxFields[2])) ?? idxDocument.AllFields.SingleOrDefault(x =>
                    x.Key.Equals("EMS DocLibrary Source", StringComparison.InvariantCultureIgnoreCase)))?.Value.Trim()
                };

                for (var i = 1; i < 30; i++)
                {
                    var segment = idxDocument.AllFields.SingleOrDefault(c => c.Key.Equals($"{_textSegment}{i}"))?.Value?.Trim();

                    if (!string.IsNullOrWhiteSpace(segment))
                    {
                        if (segment.EndsWith(".msg") || segment.EndsWith(".zip:Tasks") || segment.EndsWith(".zip:Emails"))
                        {
                                break;                            
                        }

                        if (!segment.Contains(".msg:"))
                        {
                            emsFolder.VIRTUAL_PATH_SEGMENTs.Add(segment.Replace(":", "-").Trim());
                        }
                    }
                }

                _observedEmsFolders.Add(emsFolder);
                _locationFileWriter.OutputRecord(idxDocument.DocumentId, emsFolder.ConvertedEdtLocation);

                if (!emsFolder.ConvertedEdtLocation.ReplaceTagChars().Equals(edtLocation.ReplaceTagChars(), StringComparison.InvariantCultureIgnoreCase)  
                && !edtLocation.ReplaceTagChars().StartsWith(emsFolder.LocationStem.ReplaceTagChars()))
                {
                    TestResult.Different++;
                    TestResult.AddComparisonResult(idxDocument.DocumentId, edtLocation, emsFolder.ConvertedEdtLocation, emsFolder.ConvertedEdtLocation);
                }
                else
                {
                    TestResult.Matched++;
                }


            });
        }

        private void PrintObservedLocations(IEnumerable<EmsFolder> emsFolders)
        {
            using (var writer = new StreamWriter(Path.Combine(Settings.ReportingDirectory, "locations_observedIdxRawValues.csv")))
            {
                writer.WriteLine("Group, Custodians, Source, Virtual Path Segments");

                emsFolders
                    .Distinct()
                    .ToList()
                    .ForEach(x => writer.WriteLine($"{x.Group},{x.Custodian},{x.Source},{x.VirtualPathSegements}"));
            }
        }

        public void Dispose()
        {
           
            try
            {
                PrintObservedLocations(_observedEmsFolders);
                _locationFileWriter?.Dispose();
            }
            catch (Exception e)
            {
                DebugLogger.Instance.WriteException(e);
                //Console.WriteLine(e);
                //throw;
            }
        }
    }
}
