using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    public class Reports
    {
        [XmlElement(ElementName = "customreport")]
        public CustomReport[] CustomReports { get; set; }

        [XmlArray("dashboardreports")]
        [XmlArrayItem("dashboardreport")]
        public DashboardReport[] DashboardReports { get; set; }
    }
}
