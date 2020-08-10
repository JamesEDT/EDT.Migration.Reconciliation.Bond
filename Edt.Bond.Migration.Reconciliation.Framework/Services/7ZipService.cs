using MoreLinq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class _7ZipService
    {
        public Dictionary<string,string> GetFiles(string zipPath, bool GettingNatives = true)
        {

            var files = new List<string>();
            ProcessStartInfo p = new ProcessStartInfo
            {
                FileName = @"c:\Program Files\7-Zip\7z.exe",
                Arguments = "l -r -ba \"" + zipPath + "\"",
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = true

            };

            Process x = Process.Start(p);

			while (!x.StandardOutput.EndOfStream)
			{
				string line = x.StandardOutput.ReadLine();


                if (!string.IsNullOrWhiteSpace(line) && !line.Contains("D...."))
                {
                    var file = line.Substring(53);
                    files.Add(file);
                }

                
				// do something with line
			}

            var typeSpecifc = files.Where(f => f.Contains(GettingNatives ? "NATIVE" : "TEXT")).ToList();

            PrintFileList(typeSpecifc, GettingNatives);

            var dictionaryOfFiles = new Dictionary<string, string>();

           typeSpecifc.ForEach(f =>
           {
               var key = Path.GetFileNameWithoutExtension(f);
               if (!dictionaryOfFiles.Keys.Contains(f))
                   dictionaryOfFiles.Add(key, f);
           });

           return dictionaryOfFiles;
		}

        private void PrintFileList(List<string> files, bool isNatives)
        {
            using (var writer = new StreamWriter(Path.Combine(Settings.ReportingDirectory, $"zipContents{(isNatives ? "NATIVE" : "TEXT")}.csv")))
            {
                files.ForEach(writer.WriteLine);
            }
        }
    }
}
