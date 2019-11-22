using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    [XmlType("folder")]
    public class Folder
    {
        [XmlElement(ElementName = "id")]
        public int Id { get; set; }

        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "foldertype")]
        public string Type { get; set; }

        [XmlElement(ElementName = "permission")]
        public Permission[] Permissions;

        [XmlArray(ElementName = "folders", IsNullable = true)]
        [XmlArrayItem(ElementName = "folder")]
        public Folder[] SubFolders { get; set; }
    }
}
