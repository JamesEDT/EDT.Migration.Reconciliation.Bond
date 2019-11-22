namespace Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase
{
    public class MvField
    {
        public int MvFieldID { get; set; }
        public string Name { get; set; }
        public int ParentID { get; set; }
        public int DisplayOrder { get; set; }
        public byte AdminAccessLevel { get; set; }
    }
}
