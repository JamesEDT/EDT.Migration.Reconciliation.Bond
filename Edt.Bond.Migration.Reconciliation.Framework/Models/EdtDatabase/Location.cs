namespace Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase
{
    public class Location
    {
        public int LocationID { get; set; }
        public int RootID { get; set; }
        public int ParentID { get; set; }
        public int BatchID { get; set; }
        public string Path { get; set; }
        public string ContainerTypes { get; set; }
        public bool DisplayInReviewer { get; set; }
        public string PathForSearch { get; set; }
        public string PhysicalFullPath { get; set; }
    }
}
