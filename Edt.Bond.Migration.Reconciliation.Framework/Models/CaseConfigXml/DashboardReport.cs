using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    [XmlType("dashboardreport")]
    public class DashboardReport
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "specialtype")]
        public string SpecialType { get; set; }

        [XmlElement(ElementName = "ismatterdashboard")]
        public bool IsMatterDashboard { get; set; }

        [XmlElement(ElementName = "columnindex")]
        public int ColumnIndex { get; set; }

        [XmlElement(ElementName = "pageindex")]
        public int PageIndex { get; set; }

        [XmlElement(ElementName = "username")]
        public string Username { get; set; }

        [XmlElement(ElementName = "reportname")]
        public string ReportName { get; set; }
    }
}
