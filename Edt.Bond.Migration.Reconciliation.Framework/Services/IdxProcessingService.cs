using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class IdxProcessingService
    {
        private static StreamReader _streamReader;
        private const long ProcessingChunkSize = 100;
        private static readonly string[] DocumentEndTag = new string[] {"#DREENDDOC"};
        

        public static IEnumerable<Document> GetNextDocumentChunkFromFile(string filepath, bool removeEncapsulation = false)
        {
            if (_streamReader == null) 
                _streamReader = new StreamReader(filepath);

            var stringBuilder = new StringBuilder();
            var docsSeen = 0;

            while(!_streamReader.EndOfStream && docsSeen <= ProcessingChunkSize)
            {
                var line = _streamReader.ReadLine();

                if(line != null && !line.StartsWith("#DRE"))
                    line = $"\r\n{line}";

                if (line != null && line.Contains(DocumentEndTag[0]))
                    docsSeen++;

                stringBuilder.Append(line);
            }
            return stringBuilder.ToString().Split(DocumentEndTag, StringSplitOptions.RemoveEmptyEntries).Select(x => new Document(x, removeEncapsulation));
        }
    }
}
