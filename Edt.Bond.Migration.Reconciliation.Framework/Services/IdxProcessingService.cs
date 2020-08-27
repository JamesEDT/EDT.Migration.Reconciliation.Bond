using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Edt.Bond.Migration.Reconciliation.Framework.Extensions;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using HtmlTags;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class IdxReaderByChunk : IDisposable
    {
        private readonly int _chunkSize = 600000;
        private readonly int _lineSize = 2000;
        private readonly StreamReader _streamReader;
        private readonly string[] _documentEndTag = new string[] { "#DREENDDOC" };
        private const string DocumentStartTag = "#DREREFERENCE";
        private StringBuilder _lastTokenFromPreviousBatch = null;

        public bool EndOfFile;
        private bool _readInChunks = false;
        
        

        public IdxReaderByChunk(StreamReader streamReaderReader)
        {
            _streamReader = streamReaderReader;
        }

        public IEnumerable<Document> GetNextDocumentBatch()
        {
            return _readInChunks ? GetNextDocumentBatchByChunk() : GetNextDocumentBatchByLine();
        }
        public IEnumerable<Document> GetNextDocumentBatchByChunk()
        {
            var buffer = new char[_chunkSize];

            var bytesRead = _streamReader.ReadBlock(buffer, 0, buffer.Length);

            if (bytesRead > 0)
            {
                EndOfFile = bytesRead < _chunkSize;
                var str = new string(buffer);

                if (str.Contains(_documentEndTag[0]))
                {
                    if (_lastTokenFromPreviousBatch != null)
                    {
                        str = $"{_lastTokenFromPreviousBatch}{str}";
                    }

                    List<string> tokens;
                    try
                    {
                        tokens = str.Split(_documentEndTag, StringSplitOptions.RemoveEmptyEntries).ToList();
                    }

                    catch(OutOfMemoryException)
                    {
                        //remove content and try split again
                        tokens = str.LowMemSplit(_documentEndTag.First());

                    }
                   
                    _lastTokenFromPreviousBatch = new StringBuilder(tokens.Last());

                    tokens.RemoveAt(tokens.Count - 1);

                    return tokens
                        .AsParallel()
                        .Select(x => new Document(x));

                }
                else
                {
                    _lastTokenFromPreviousBatch = _lastTokenFromPreviousBatch.Append(str);

                }
            }
            else
            {

                EndOfFile = true;
            }

            return null;            
        }

        public IEnumerable<Document> GetNextDocumentBatchByLine()
        {
            var stringBuilder = new StringBuilder();
            var docsSeen = 1;
            var docsToReturn = new List<Document>();
            var seenDreContent = false;

            while (!_streamReader.EndOfStream && docsSeen <= _lineSize)
            {
                var line = _streamReader.ReadLine();
                if (line != null)
                {
                    if (!seenDreContent && line.StartsWith("#DRECONTENT"))
                    {
                        seenDreContent = true;
                    }
                    else
                    {
                        if (seenDreContent && line.StartsWith("#DRE"))
                        {
                            seenDreContent = false;
                        }

                        if (!seenDreContent)
                        {
                            
                            stringBuilder.Append(line.StartsWith("#DRE") ? line : $"\r\n{line}");
                            if (line.StartsWith(_documentEndTag[0]))
                            {
                                docsSeen++;
                                docsToReturn.Add(new Document(stringBuilder.ToString()));
                                stringBuilder = new StringBuilder();
                            }
                        }
                    }
                }
            }

            EndOfFile = _streamReader.EndOfStream;

            return docsToReturn;
        }


        public void Dispose()
        {
            _streamReader?.Close();
        }

        public List<string> GetDocumentIds()
        {
            var docIDs = new List<string>();
            var splitArray = "=".ToCharArray();

            while (!_streamReader.EndOfStream)
            {

                var line = _streamReader.ReadLine();

                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("#DREFIELD UUID=")) continue;

                var str = line.Split(splitArray).LastOrDefault()?.Replace("\"", string.Empty);

                docIDs.Add(str);
            }

            return docIDs;
        }

        public List<string> GetQuarantinedDocs()
        {
            var docIDs = new List<string>();
            var splitArray = "=".ToCharArray();

            var currentId = "";

            while (!_streamReader.EndOfStream)
            {

                var line = _streamReader.ReadLine();

                if (string.IsNullOrWhiteSpace(line) || !line.Contains("UUID=") && !line.Contains("INTROSPECT_DELETED=\"")) continue;

                if (line.Contains("UUID"))
                {
                    currentId = line.Split(splitArray).LastOrDefault()?.Replace("\"", string.Empty);
                }
                else 
                {
                    docIDs.Add(currentId);
                }                
            }

            return docIDs;
        }
    }
}
