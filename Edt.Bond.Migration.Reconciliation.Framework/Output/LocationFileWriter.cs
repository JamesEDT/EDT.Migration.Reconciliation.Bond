using System;
using System.IO;

namespace Edt.Bond.Migration.Reconciliation.Framework.Output
{
    public class LocationFileWriter: IDisposable
    {
        private string _fileStem = "Location";

        private string _encap = "\"";
        private string _delimiter = "\t";

        public StreamWriter _streamWriter;

        public LocationFileWriter()
        {
            _streamWriter = new StreamWriter(Path.Combine(Settings.ReportingDirectory, $"{_fileStem}.txt"));
            _streamWriter.WriteLine($"Document Id{_delimiter}Location");
        }
        

        public void OutputRecord(string docNumber, string record)
        {

	        if (Settings.GenerateLoadFile)
	        {
			    _streamWriter.WriteLine($"{_encap}{record ?? string.Empty}{_encap}");
	        }
        }

        public void Dispose()
        {
            _streamWriter?.Close();
        }
    }
}
