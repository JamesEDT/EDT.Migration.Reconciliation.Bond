namespace Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase
{
    public class Kind
    {
        public int KindID { get; set; }
        public int SuperKind { get; set; }
        public string MainKind { get; set; }
        public string Description { get; set; }
        public string FileExtOrMsgClass { get; set; }
    }
}
