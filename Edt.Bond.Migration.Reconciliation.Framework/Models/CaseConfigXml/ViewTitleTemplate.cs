using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    public class ViewTitleTemplate
    {
        [XmlAttribute("value")]
        public string Value { get; set; }
    }
}