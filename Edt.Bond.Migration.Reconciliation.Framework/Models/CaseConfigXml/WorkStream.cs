using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    [XmlType("workstream")]
    public class WorkStream
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "streamType")]
        public string StreamType { get; set; }
        [XmlElement(ElementName = "description")]
        public string Description { get; set; }
    }
}