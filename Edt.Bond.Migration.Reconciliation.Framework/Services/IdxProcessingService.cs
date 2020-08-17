﻿using System;
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
        private readonly int _chunkSize = 12000000;
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
            }
            else
            {

                EndOfFile = true;
            }

            return null;            
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
