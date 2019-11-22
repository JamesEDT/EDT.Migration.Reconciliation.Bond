namespace Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile
{
    public class Field
    {
        public int Id { get; set; }
        public string UUID { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        public Field(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public Field()
        {

        }
    }
}
