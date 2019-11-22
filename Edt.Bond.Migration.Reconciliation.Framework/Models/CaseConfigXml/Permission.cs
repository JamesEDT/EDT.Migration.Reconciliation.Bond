using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    public class Permission
    {
        [XmlAttribute(AttributeName = "role")]
        public string Role { get; set; }

        [XmlAttribute(AttributeName = "values")]
        public string Values { get; set; }
    }
}
