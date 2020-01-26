using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Framework.Output
{
    public class FullLoadFileWriter: IDisposable
    {
        private string _fileStem = "LoadFileFull";

        private string _encap = "\"";
        private string _delimiter = "\t";

        public StreamWriter _streamWriter;

        public FullLoadFileWriter()
        {
            _streamWriter = new StreamWriter(Path.Combine(Settings.ReportingDirectory, $"{_fileStem}.txt"));
        }

        public void OutputHeaders(List<string> headers)
        {
            if (Settings.GenerateLoadFile)
            {
                var joined = string.Join(_delimiter, headers);

                _streamWriter.WriteLine($"document id{_delimiter}{joined}");
            }
        }

        public void OutputRecord(string documber, IEnumerable<string> values)
        {
	        if (Settings.GenerateLoadFile)
	        {
                var joined = string.Join(_delimiter, values.Select(x => $"{_encap}{x}{_encap}"));

			    _streamWriter.WriteLine($"{documber}{_delimiter}{joined}");
	        }
        }

        public void Dispose()
        {
            _streamWriter?.Close();
        }
    }
}
