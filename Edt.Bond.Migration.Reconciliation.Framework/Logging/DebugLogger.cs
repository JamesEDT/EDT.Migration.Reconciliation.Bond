using System;
using System.IO;

namespace Edt.Bond.Migration.Reconciliation.Framework.Logging
{
    public class DebugLogger : IDisposable
    {
        private StreamWriter _streamWriter;
        private bool _isClosed;

        public static DebugLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DebugLogger();
                }

                return _instance;

            }
        }

        private static DebugLogger _instance;
        

        public DebugLogger()
        {
            InitialiseLog();
        }

        private void InitialiseLog()
        {
            ////lock (_streamWriter)
           // {
                _streamWriter = new StreamWriter(Path.Combine(Settings.LogDirectory, $"DebugLog_{Settings.EdtCaseId}_{DateTime.Now.Ticks}.txt"));
                _streamWriter.AutoFlush = true;
            _isClosed = false;
           // }
        }

        public void WriteLine(string line)
        {
            lock (_streamWriter)
            {
                if (_isClosed) InitialiseLog();

                _streamWriter.WriteLine(line);
            }
        }

        public void WriteException(Exception e, string comment = null)
        {
            lock (_streamWriter)
            {
                if(_isClosed) InitialiseLog();

                if (comment != null)
                    _streamWriter.WriteLine(comment);

                _streamWriter.WriteLine(e.Message);
                _streamWriter.WriteLine(e.StackTrace);

                if (e.InnerException != null)
                {
                    _streamWriter.WriteLine(e.InnerException.Message);
                    _streamWriter.WriteLine(e.InnerException.StackTrace);
                }
            }
        }

        public void Dispose()
        {
            lock (_streamWriter)
            {
                _isClosed = true;
                _streamWriter?.Close();
            }
        }
    }
}
