using System;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase
{
    public class DocumentLanguage
    {
        public int DocumentLanguageID { get; set; }
        public int DocumentID { get; set; }
        public int LanguageID { get; set; }
        public Single Proportion { get; set; }
        public Single Confidence { get; set; }
        public bool IsPrimaryLanguage { get; set; }
    }
}
