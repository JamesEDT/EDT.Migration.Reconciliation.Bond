using System;
using System.Collections.Generic;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile
{
    public class Document
    {
        public long Id { get; set; }
        public string Reference { get; set; }
        public string Title { get; set; }
        public string Date { get; set; }
        public string Section { get; set; }
        public string Dbname { get; set; }
        public string Content { get; set; }

        public string PARENTREFERENCE_ID { get; set; }
        public string UUID { get; set; }
        public string DREROOTMSGREFERENCE_ID { get; set; }

        public long CHILDCOUNT { get; set; }

        public List<Field> Fields { get; set; }

        public List<string> Unknowns { get; set; }

        public List<Field> AllFields { get; set; }

        public Document()
        {

        }

        public Document(string raw, bool removeEncapsulation = false)
        {
            Fields = new List<Field>();
            AllFields = new List<Field>();
            Unknowns = new List<string>();

            raw.Split(new string[]{"#DRE"}, StringSplitOptions.RemoveEmptyEntries).AsParallel().ForAll(GenerateFieldAndAddToFields);           

        }

        public string GetFileName()
        {
            var filename = AllFields.FirstOrDefault(x => x?.Key == "UUID")?.Value;

            if (string.IsNullOrEmpty(filename))
                throw new Exception("Couldnt Determine Filename for document");

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
