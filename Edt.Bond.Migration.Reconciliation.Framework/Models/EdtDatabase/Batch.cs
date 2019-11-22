using System;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase
{
    public class Batch
    {
        public int BatchID { get; set; }
        public string BatchName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime CompletedAtUtc { get; set; }
        public int PdfCount { get; set; }
        public long PdfSize { get; set; }
        public int NativeCount { get; set; }
        public long NativeSize { get; set; }
        public string Settings { get; set; }
        public string FieldMapping { get; set; }
        public DateTime RemovedDateUtc { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsImport { get; set; }
        public int ProductVersion { get; set; }
    }
}
