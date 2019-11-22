using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    public class Folders
    {
        [XmlElement(ElementName = "folder")]
        public Folder[] Items { get; set; }
    }
}
