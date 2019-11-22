using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    public class CustomReport
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "filterquery")]
        public string FilterQuery { get; set; }

        [XmlElement(ElementName = "reportquery")]
        public string ReportQuery { get; set; }

        [XmlElement(ElementName = "username")]
        public string Username { get; set; }

        [XmlElement(ElementName = "global")]
        public bool IsGlobal { get; set; }

        [XmlElement(ElementName = "total")]
        public bool IsTotal { get; set; }

        [XmlElement(ElementName = "isbasespecialreport")]
        public bool IsBaseSpecialReport { get; set; }

        [XmlElement(ElementName = "permission")]
        public Permission[] Permissions;
    }
}
