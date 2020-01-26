using System;
using System.IO;

namespace Edt.Bond.Migration.Reconciliation.Framework.Logging
{
    public class DebugLogger : IDisposable
    {
        private StreamWriter _streamWriter;

        public static DebugLogger Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new DebugLogger();

                return _instance;

            }
        }

        private static DebugLogger _instance;

        public DebugLogger()
        {
            _streamWriter = new StreamWriter(Path.Combine(Settings.LogDirectory, $"DebugLog_{Settings.EdtCaseId}.txt"));
        }

        public void WriteException(Exception e, string comment = null)
        {
            if (comment != null)
                _streamWriter.WriteLine(comment);

            _streamWriter.WriteLine(e.Message);
            _streamWriter.WriteLine(e.StackTrace);

            if(e.InnerException != null)
            {
                _streamWriter.WriteLine(e.InnerException.Message);
                _streamWriter.WriteLine(e.InnerException.StackTrace);
            }
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
