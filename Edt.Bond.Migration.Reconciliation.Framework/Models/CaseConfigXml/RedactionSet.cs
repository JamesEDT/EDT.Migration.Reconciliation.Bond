using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    public class RedactionSet
    {
        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlArray("Settings")]
        [XmlArrayItem("Setting")]
        public Setting[] Settings { get; set; }

        [XmlArray("Reasons")]
        [XmlArrayItem("Reason")]
        public Reason[] Reasons { get; set; }
    }
}