using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edt.Bond.Migration.BatchRecordUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Direct Record Updater");
            Console.WriteLine($"Configured against {ConfigurationManager.AppSettings["EdtCaseDatabaseName"]}");

            while (true)
            {

                Console.Write("Input File:");

                var inputFile = Console.ReadLine();

                if(inputFile.StartsWith("\"") && inputFile.EndsWith("\"")){
                    inputFile = inputFile.Trim(new char[] { '"' });
                }

                if (!File.Exists(inputFile))
                {
                    Console.WriteLine("Error: file doesnt exist");
                }
                else
                {
                    using (var reader = new StreamReader(inputFile))
                    {
                        var splitChar = ",".ToCharArray();
                        var headerTokens = reader.ReadLine().Split(splitChar);
                        var updatingColumn = ConvertDisplayNameToColumn(headerTokens[1]);
                        //

                        var updatingLines = reader.ReadToEnd().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

                        updatingLines.AsParallel()
                            .ForAll(updateRecord =>
                            {
                                var updateTokens = updateRecord.Split(splitChar);
                                var updateValue = updateTokens[1].StartsWith("\"") && updateTokens[1].EndsWith("\"") ? updateTokens[1].Trim("\"".ToCharArray()) : updateTokens[1];
;                               EdtDocumentRepository.UpdateDocumentField(updateTokens[0], updatingColumn, updateValue);

                            });

                    }

                    Console.WriteLine("Update Complete");
                }
            }
        }

        public  static string ConvertDisplayNameToColumn(string displayName)
        {
            var columns = EdtDocumentRepository.GetColumnDetails();

            return columns.Single(x => x.DisplayName.Equals(displayName, StringComparison.InvariantCultureIgnoreCase)).ColumnName;

        }
    }
}
