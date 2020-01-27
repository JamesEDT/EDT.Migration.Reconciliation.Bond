using System;
using System.IO;

namespace Edt.Bond.Migration.Reconciliation.Framework.Output
{
    public class LoadFileWriter: IDisposable
    {
        private string _fileStem = "LoadFile";

        private string _encap = "\"";
        private string _delimiter = "\t";
        private bool _shouldOutput;

        public StreamWriter _streamWriter;
        public StreamReader _streamReader;

        public LoadFileWriter()
        {
            if (Settings.GenerateLoadFile)
            {
                if (File.Exists(Path.Combine(Settings.ReportingDirectory, $"{_fileStem}.txt")))
                    _streamReader = new StreamReader(Path.Combine(Settings.ReportingDirectory, $"{_fileStem}.txt"));

                _streamWriter = new StreamWriter(Path.Combine(Settings.ReportingDirectory, $"{_fileStem}_temp.txt"));

                _shouldOutput = true;
            }
        }
        
        public void OutputHeader(string column)
        {
            if (_shouldOutput)
            {
                var line = _streamReader != null ? $"{_streamReader.ReadLine()}{_delimiter}" : $"DocNumber{_delimiter}";

                _streamWriter.WriteLine($"{line}{column}");
            }
        }

        public void OutputRecord(string docNumber, string record)
        {
	        if (_shouldOutput)
	        {

		        var line = _streamReader != null
			        ? $"{_streamReader.ReadLine()}{_delimiter}"
			        : $"{_encap}{docNumber}{_encap}{_delimiter}";

		        if (line.Contains(docNumber))
		        {
			        _streamWriter.WriteLine($"{line}{_encap}{record ?? string.Empty}{_encap}");
		        }
		        else
		        {
			        throw new Exception("Tried to output for different doc");
		        }
	        }
        }

        public void Dispose()
        {
            _streamReader?.Close();
            _streamWriter?.Close();

            if (_shouldOutput)
            {
                if (File.Exists(Path.Combine(Settings.ReportingDirectory, $"{_fileStem}.txt")))
                    File.Delete(Path.Combine(Settings.ReportingDirectory, $"{_fileStem}.txt"));

                File.Move(Path.Combine(Settings.ReportingDirectory, $"{_fileStem}_temp.txt"), Path.Combine(Settings.ReportingDirectory, $"{_fileStem}.txt"));

            }
        }
    }
}
