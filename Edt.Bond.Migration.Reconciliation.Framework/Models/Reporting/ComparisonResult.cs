namespace Edt.Bond.Migration.Reconciliation.Framework.Models.Reporting
{
    public class ComparisonResult
    {
        public string DocumentId { get; set; }
        public string EdtValue { get; set; }
        public string IdxConvertedValue { get; set; }
        public string IdxValue { get; set; }

        public ComparisonResult(string documentId, string edtValue, string idxConverted, string idxValue)
        {
            DocumentId = documentId;
            EdtValue = edtValue;
            IdxConvertedValue = idxConverted;
            IdxValue = idxValue;
        }

        public string[] ToTableRow()
        {
            return new string[] { DocumentId, IdxValue, IdxConvertedValue, EdtValue };
        }
    }
}
