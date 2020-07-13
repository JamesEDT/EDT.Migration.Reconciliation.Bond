using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class IdxReaderByChunk : IDisposable
    {
        private readonly int _chunkSize = 10000000;
        private readonly StreamReader _streamReader;
        private readonly string[] _documentEndTag = new string[] { "#DREENDDOC" };
        private const string DocumentStartTag = "#DREREFERENCE";
        private string _lastTokenFromPreviousBatch = null;

        public bool EndOfFile;

        public IdxReaderByChunk(StreamReader streamReaderReader)
        {
            _streamReader = streamReaderReader;
        }

        public IEnumerable<Document> GetNextDocumentBatch()
        {
            var buffer = new char[_chunkSize];

            var bytesRead = _streamReader.ReadBlock(buffer, 0, buffer.Length);

            if (bytesRead > 0)
            {
                var str = new string(buffer);

                if (str.Contains(_documentEndTag[0]))
                {
                    var tokens = str.Split(_documentEndTag, StringSplitOptions.RemoveEmptyEntries).ToList();

                    if (_lastTokenFromPreviousBatch != null)
                    {
                        if (tokens[0].StartsWith(DocumentStartTag))
                        {
                            tokens.Add(_lastTokenFromPreviousBatch);
                        }
                        else
                        {
                            tokens[0] = $"{_lastTokenFromPreviousBatch}{tokens[0]}";
                        }
                    }

                    _lastTokenFromPreviousBatch = tokens.Last();

                    tokens.RemoveAt(tokens.Count - 1);

                    return tokens
                        .AsParallel()
                        .Select(x => new Document(x));

                }
                else
                {
                    _lastTokenFromPreviousBatch = $"{_lastTokenFromPreviousBatch}{str}";

                }

                EndOfFile = bytesRead < _chunkSize;
            }

            return null;
        }


        public void Dispose()
        {
            _streamReader?.Close();
        }
    }
}
