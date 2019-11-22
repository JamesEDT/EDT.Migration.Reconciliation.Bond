using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    [XmlType("codingrule")]
    public class CodingRule
    {

        [XmlElement(ElementName = "sourcename")]
        public string SourceName { get; set; }

        [XmlElement(ElementName = "sourcevalue")]
        public string SourceValue { get; set; }

        [XmlElement(ElementName = "sortorder")]
        public int SortOrder { get; set; }

        [XmlElement(ElementName = "requirement")]
        public string Requirement { get; set; }

        [XmlElement(ElementName = "targetName")]
        public string targetName { get; set; }

        [XmlElement(ElementName = "targetvalue")]
        public string targetValue { get; set; }
    }
}