namespace Edt.Bond.Migration.Reconciliation.Framework.Models.Reporting
{
    public class ComparisonError
    {
        public string DocumentId { get; set; }
        public string ErrorMessage { get; set; }

        public ComparisonError(string documentId, string errorMessage)
        {
            DocumentId = documentId;
            ErrorMessage = errorMessage;
        }

        public string[] ToTableRow()
        {
            return new string[] { DocumentId, ErrorMessage };
        }
    }
}
