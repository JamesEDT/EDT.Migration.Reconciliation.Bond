using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    public class Reason
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("enabled")]
        public bool Enabled { get; set; }
    }
}