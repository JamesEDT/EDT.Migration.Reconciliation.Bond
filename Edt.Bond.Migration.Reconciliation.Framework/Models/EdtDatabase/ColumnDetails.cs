using System;
using System.Collections.Generic;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase
{
    public class ColumnDetails
    {
        public int ColumnDetailsID { get; set; }
        public string ColumnName { get; set; }
        public string DisplayName { get; set; }
        public string DataType { get; set; }
        public bool? IsCustomColumn { get; set; }
        public bool? IsConfigurable { get; set; }
        public bool? IsFullText { get; set; }
        public string PickListValues { set { PickList = value.Split(new string[]{";"}, StringSplitOptions.RemoveEmptyEntries).ToList(); } }
        public List<string> PickList { get; set; }
        public string ExportDataType { get; set; }
        public string ExportValueFieldName { get; set; }
        public bool? IsRequired { get; set; }
        public string ExportDisplayName { get; set; }
       
    }
}
