using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    [XmlType("role")]
    public class Role
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("values")]
        public string Values { get; set; }
    }
}