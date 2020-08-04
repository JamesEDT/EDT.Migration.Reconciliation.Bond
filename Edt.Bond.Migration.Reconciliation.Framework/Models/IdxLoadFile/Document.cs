using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Localization.Internal;
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

            tokens
                //.AsParallel()
                .ToList().ForEach(GenerateFieldAndAddToFields);

            DocumentId = GetFileName();
        }

        public IEnumerable<string> GetValuesForIdolFields(List<string> desiredFields)
        {
            return AllFields.Where(x => desiredFields.Contains(x.Key)).Select(x => x.Value);
        }

        public string GetFileName()
        {
            var filename = AllFields.FirstOrDefault(x => x.Key.ToUpper() == "UUID")?.Value.Trim();

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
            var delimiter = rawItem.StartsWith("FIELD") ? "=" : " ";
            var field = GenerateField(rawItem, delimiter);

            if(!string.IsNullOrWhiteSpace(field.Value))
                AllFields.Add(field);
        }

        private Field GenerateField(string rawItem, string delimiter = "=", bool removeEncapsulation = true)
        {
            rawItem = rawItem.StartsWith("FIELD ") ? rawItem.Remove(0, 6) : $"DRE{rawItem}";

            var tokens = rawItem.Split(new string[] { delimiter }, StringSplitOptions.None).ToList();

            var key = tokens.FirstOrDefault();
            tokens.Remove(key);

            var valueString = string.Join(delimiter, tokens).TrimEnd(new char[] { '\r', '\n' });

            if (removeEncapsulation)
            {
                var fieldEncapsulationChar = new char[] { '"' };

                if (valueString.StartsWith("\"") && valueString.EndsWith("\""))
                    valueString = valueString.Substring(1, valueString.Length - 2);
            }

            return new Field(key, valueString.TrimEnd(new char[] { '\r', '\n' }));
        }
    }
}
