using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion
{
    public class StandardMapping
    {
        public string EdtName { get; set; }
        public string IdxName { get; set; }
        public string EdtType { get; set; }
        public string ImportGroup { get; set; }

        public bool IsEmailField()
        {
            return EdtName.StartsWith(Settings.EmailFieldIdentifyingPrefix);
        }
    }
}
