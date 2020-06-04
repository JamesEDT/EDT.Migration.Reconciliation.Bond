using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase;
using System.Collections.Generic;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion
{
    public class StandardMapping
    {
        public string EdtName { get; set; }
        public List<string> IdxNames { get; set; }
        public string EdtType { get; set; }
        public string ImportGroup { get; set; }

        public StandardMapping(string edtName, string idxName, string edtType, string importGroup)
        {
            IdxNames = new List<string>() {idxName};
            EdtName = edtName;
            EdtType = edtType;
            ImportGroup = importGroup;
            
        }

        public StandardMapping(string edtName, List<string> idxNames, string edtType, string importGroup)
        {
            IdxNames = idxNames;
            EdtName = edtName;
            EdtType = EdtType;
            ImportGroup = importGroup;
        }

        public bool IsEmailField()
        {
            return EdtName.StartsWith(Settings.EmailFieldIdentifyingPrefix);
        }
    }
}
