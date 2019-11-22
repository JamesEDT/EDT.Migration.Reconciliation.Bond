using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    public class Field
    {
        [XmlElement("id")]
        public string Id { get; set; }

        [XmlElement("displayName")]
        public string DisplayName { get; set; }

        [XmlElement("columnRenderingType")]
        public string ColumnRederingType { get; set; }

        [XmlElement("idolName")]
        public string IdolName { get; set; }

        [XmlElement("searchType")]
        public string SearchType { get; set; }

        [XmlElement("searchRenderingType")]
        public string SearchRenderingType { get; set; }

        [XmlElement("isParametric")]
        public bool IsParametric { get; set; }

        [XmlElement("isMultiValued")]
        public bool IsMultiValued { get; set; }

        [XmlElement("documentMatchFieldOrder")]
        public int DocumentMatchFieldOrder { get; set; }

        [XmlElement("documentDisplayFieldOrder")]
        public int DocumentDisplayFieldOrder { get; set; }

        [XmlElement("advancedSearchOrder")]
        public int AdvancedSearchOrder { get; set; }

        [XmlElement("filterFieldOrder")]
        public int FilterFieldOrder { get; set; }

        [XmlElement("columnOrder")]
        public int columnOrder { get; set; }

        [XmlElement("dateFormatIn")]
        public string dateFormatIn { get; set; }

        [XmlElement("dateFormatOut")]
        public string dateFormatOut { get; set; }

        [XmlElement("operations")]
        public string Operations { get; set; }

        [XmlElement("permission")]
        public Permission[] Permissions { get; set; }

    }
}