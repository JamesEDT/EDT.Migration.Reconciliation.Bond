using System;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase
{
    public class Message
    {
        public int MessageID { get; set; }
        public int BatchID { get; set; }
        public string ItemType { get; set; }
        public int ItemID { get; set; }
        public int Operation { get; set; }
        public string MessageLevel { get; set; }
        public string Status { get; set; }
        public string MessageText { get; set; }
        public string Extra { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
 }
