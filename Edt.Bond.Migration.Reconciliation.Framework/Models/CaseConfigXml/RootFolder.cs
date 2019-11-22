using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    public class RootFolder
    {
        [XmlElement(ElementName = "permission")]
        public Permission[] Permissions;
    }
}
