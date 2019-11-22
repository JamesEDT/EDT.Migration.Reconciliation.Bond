using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    public class TagsContainer
    {

        [XmlElement(ElementName = "tag")]
        public Tag[] Tags { get; set; }

        [XmlArray("taggroups")]
        [XmlArrayItem("tagroup")]
        public TagGroup[] TagGroups { get; set; }
    }

    public class TagGroup
    {
        [XmlElement(ElementName = "id")]
        public int Id { get; set; }

        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "description")]
        public string Description { get; set; }

        [XmlArray("taggroups")]
        [XmlArrayItem("tagroup")]
        public TagGroup[] TagGroups { get; set; }
    }

    public class ChildTag
    {
        [XmlElement(ElementName = "id")]
        public int Id { get; set; }
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }
    }

    public class Tag
    {
        [XmlElement(ElementName = "id")]
        public int Id { get; set; }

        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "displayType")]
        public string DisplayType { get; set; }

        [XmlElement(ElementName = "sortOrder")]
        public int SortOrder { get; set; }

        [XmlElement(ElementName = "idolFieldName")]
        public string IdolFieldName { get; set; }

        [XmlElement(ElementName = "isPredictive")]
        public bool IsPredictive { get; set; }

        [XmlElement(ElementName = "isUsedForRedaction")]
        public bool IsUsedForRedaction { get; set; }

        [XmlElement(ElementName = "isSearchable")]
        public bool IsSearchable { get; set; }

        [XmlElement(ElementName = "tagType")]
        public string TagType { get; set; }

        [XmlArray("values")]
        [XmlArrayItem("value")]
        public string[] Values { get; set; }
    }
}
