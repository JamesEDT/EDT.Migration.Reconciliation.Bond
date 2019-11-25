using Edt.Bond.Migration.Reconciliation.Framework.Extensions;
using Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml;
using System;
using System.Data;
using System.IO;
using System.Linq;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class ConversionToolLoadFileReader
    {
        // from directory in settings
        // find load file
        // create array of Fields like generated Idx model

        public void Read()
        {
            var loadFilePath = DetermineLoadFilePath();

            PopulateLocalStoreFromCsv(loadFilePath);
        }

        private void PopulateLocalStoreFromCsv(string path)
        {
            var loadFileRepo = new ConversionLoadFileRepository();
            loadFileRepo.Initialise();

            using (var streamReader = new StreamReader(path))
            {
                var headers = streamReader.ReadLine()?.SplitCsv();

                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine()?.SplitCsv();

                    var allFields = headers
                        ?.Select((x, i) => new Framework.Models.IdxLoadFile.Field(x, line?[i] ?? string.Empty))
                        ?.ToList();

                    loadFileRepo.AddDocument(new Document(allFields));
                }
            }
        }

        private string DetermineLoadFilePath()
        {
            var files = Directory.GetFiles(Settings.ConversionToolOutputDirectory, "loadFile*.*", SearchOption.TopDirectoryOnly);

            if (files.Length == 1)
                return files.First();

            if (files.Length == 0)
                throw new Exception($"Couldnt find a load file in the specified conversion tool output directory {Settings.ConversionToolOutputDirectory}");

            return files.Select(x => new FileInfo(x))
                .OrderByDescending(x => x.LastWriteTimeUtc)
                .First()
                .Name;
        }
    }
}
