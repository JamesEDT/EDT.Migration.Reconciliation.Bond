using System;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase
{
    public class DocumentExtra
    {
        public int DocumentID { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public bool IsAllDay { get; set; }
        public string StartTimeZone { get; set; }
        public string MeetingLocation { get; set; }
        public string RequestOrResponseType { get; set; }
        public double GpsLatitude { get; set; }
        public double GpsLongitude { get; set; }
        public double GpsAltitude { get; set; }
    }
}
