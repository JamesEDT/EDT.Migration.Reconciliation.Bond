using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    public class ColumnField
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("order")]
        public int order { get; set; }
    }
}