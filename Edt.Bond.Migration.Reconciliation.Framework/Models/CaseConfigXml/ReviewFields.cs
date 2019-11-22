using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    public class ReviewFields
    {
        [XmlElement("field")]
        public Field[] Fields { get; set; }

        [XmlArray("advancedSearchFields")]
        [XmlArrayItem("advancedSearchField")]
        public AdvancedSearchField[] AdvancedSearchFields { get; set; }

        [XmlArray("filterFields")]
        [XmlArrayItem("filterField")]
        public FilterField[] FilterFields { get; set; }

        [XmlArray("columnFields")]
        [XmlArrayItem("columnField")]
        public ColumnField[] ColumnFields { get; set; }
    }
}