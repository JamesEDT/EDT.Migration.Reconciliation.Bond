using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile
{
    public class Document
    {
        public long Id { get; set; }

        public string DocumentId { get; set; }

        public ConcurrentBag<Field> AllFields { get; set; }

        public Document()
        {

        }

        public Document(List<Field> allFields)
        {
            AllFields = new ConcurrentBag<Field>(allFields);
            DocumentId = GetFileName();
        }

        public Document(string raw, bool removeEncapsulation = false)
        {
            AllFields = new ConcurrentBag<Field>();

            var tokens = raw.Split(new string[]{"#DRE"}, StringSplitOptions.RemoveEmptyEntries);

            tokens.AsParallel().ForAll(GenerateFieldAndAddToFields);

            DocumentId = GetFileName();
        }

        public string GetFileName()
        {
            var filename = AllFields.FirstOrDefault(x => x.Key.ToUpper() == "UUID")?.Value;

            if (string.IsNullOrEmpty(filename))
                TestContext.Out.WriteLine($"Warning: File found with no uuid with reference {AllFields.FirstOrDefault(x => x.Key.ToUpper() == "REFERENCE")?.Value}");

            return filename;
        }

        public string GetFileExtension()
        {
            var filename = AllFields.FirstOrDefault(x => x.Key == "FILETYPE_PARAMETRIC")?.Value;

            if (string.IsNullOrEmpty(filename))
            {
                filename = AllFields.FirstOrDefault(x => x.Key == "IMPORTMAGICEXTENSION")?.Value;

                if (!string.IsNullOrEmpty(filename))
                    filename = "." + filename;
            }

            if (string.IsNullOrEmpty(filename))
                throw new Exception($"Couldnt Determine File extension for document {GetFileName()}");
            
            return filename;
        }

        private void GenerateFieldAndAddToFields(string rawItem)
        {
            var delimiter = rawItem.Contains("=") ? "=" : " ";
            var field = GenerateField(rawItem, delimiter, true);

            AllFields.Add(field);
        }

        private Field GenerateField(string rawItem, string delimiter = "=", bool removeEncapsulation = false)
        {
            if (rawItem.StartsWith("FIELD "))
                rawItem = rawItem.Remove(0, 6);

            var tokens = rawItem.Split(new string[]{delimiter}, StringSplitOptions.None).ToList();

            var key = tokens.FirstOrDefault();
            tokens.Remove(key);
            var valueString = string.Join(delimiter, tokens);

            //remove all quotes
            valueString = valueString.Replace("\"", string.Empty );

            return new Field(key, valueString);
        }    
    }
}
