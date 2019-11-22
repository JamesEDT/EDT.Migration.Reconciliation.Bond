using System;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase
{
    public class DocumentContent
    {
        public Guid DocumentContentID { get; set; }
        public byte[] Content { get; set; }
        public long Size { get; set; }
        public int DocumentID { get; set; }
        public string DocumentName { get; set; }
        public string ContentType { get; set; }
        public string FileExtension { get; set; }
        public int BatchID { get; set; }
        public int PageNum { get; set; }
        public int NumPages { get; set; }
        public string PageLabel { get; set; }
        public string GenerationSettings { get; set; }
        public int Statuses { get; set; }

    }
}
