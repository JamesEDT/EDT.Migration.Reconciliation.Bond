namespace Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase
{
    public class DocumentParty
    {
        public int DocumentPartyID { get; set; }
        public int DocumentID { get; set; }
        public int PartyID { get; set; }
        public int CorrespondenceTypeID { get; set; }
    }
}
