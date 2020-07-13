using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase
{
    public class ColumnDetails
    {
        public int ColumnDetailsID { get; set; }
        public string ColumnName { get; set; }
        public string DisplayName { get; set; }
        public ColumnType DataType { get; set; }
        public bool? IsCustomColumn { get; set; }
        public bool? IsConfigurable { get; set; }
        public bool? IsFullText { get; set; }
        public string PickListValues { set { PickList = value.Split(new string[]{";"}, StringSplitOptions.RemoveEmptyEntries).ToList(); } }
        public List<string> PickList { get; set; }
        public string ExportDataType { get; set; }
        public string ExportValueFieldName { get; set; }
        public bool? IsRequired { get; set; }
        public string ExportDisplayName { get; set; }
        public int? Size { get; set; }
       

        public string GetAlphaNumbericOnlyDisplayName()
        {
            Regex rgx = new Regex("[^a-zA-Z0-9]");
            var str = rgx.Replace(DisplayName, "");
            return str;
        }
    }

    public enum ColumnType
    {
        Boolean,
        Date,
        Float,
        List,
        Memo,
        Number,
        Text,
        MultiValueList
	}
}
