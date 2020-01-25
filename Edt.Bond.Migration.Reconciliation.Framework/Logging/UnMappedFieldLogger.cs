using System;
using System.IO;

namespace Edt.Bond.Migration.Reconciliation.Framework.Logging
{
    public class UnMappedFieldLogger : IDisposable
    {
        private StreamWriter _streamWriter;

        public static UnMappedFieldLogger Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new UnMappedFieldLogger();

                return _instance;

            }
        }

        private static UnMappedFieldLogger _instance;

        public UnMappedFieldLogger()
        {
            _streamWriter = new StreamWriter(Path.Combine(Settings.LogDirectory, $"UnMappedFileds_{Settings.EdtCaseId}.txt"));
            _streamWriter.WriteLine("IdxName|EdtName");
        }

        public void WriteUnmappedFile(string idxName, string edtName)
        {
            _streamWriter.WriteLine($"{idxName}|{edtName}");
        }

        public void Dispose()
        {
            if (_streamWriter != null)
            {
                _streamWriter.Flush();
                _streamWriter.Close();
            }
        }
    }
}
