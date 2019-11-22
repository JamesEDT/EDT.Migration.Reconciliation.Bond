using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    public class DpModule
    {
        [XmlAttribute(AttributeName = "value")]
        public bool Value { get; set; }
    }
}
