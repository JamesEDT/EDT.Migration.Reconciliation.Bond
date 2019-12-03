using Edt.Bond.Migration.Reconciliation.Framework.Extensions;
using System.Collections.Generic;
using System.IO;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class AunWorkbookReader
    {
        public static Dictionary<string,string> Read()
        {
            var aunPath = Path.Combine(Settings.MicroFocusSourceDirectory, "AUN_WORKBOOK.CSV");

            using (var streamReader = new StreamReader(aunPath))
            {
                var headers = streamReader.ReadLine();

                var aun_folders = new Dictionary<string, string>();

                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine()?.SplitCsv();

                    aun_folders.Add(line[0], line[1]);
                }

                return aun_folders;
            }            
        }
    }
}
