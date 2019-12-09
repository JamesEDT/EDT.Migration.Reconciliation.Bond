using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class IdxToEdtConversionService
    {
        private StandardMapping _standardMapping;
        private ColumnDetails _edtColumnDetails;

        public string MappedEdtDatabaseColumn => _edtColumnDetails?.ColumnName;

        public ColumnType? MappedEdtDatabaseColumnType => _edtColumnDetails?.DataType;
       
        public IdxToEdtConversionService(StandardMapping standardMapping)
        {
            _standardMapping = standardMapping;

            if (!_standardMapping.IsEmailField())
                _edtColumnDetails = GetEdtColumnDetailsFromDisplayName(standardMapping.EdtName);
        }

        public string ConvertValueToEdtForm(string value)
        {
            switch (_edtColumnDetails?.DataType)
            {
                case ColumnType.Date:
                    return GetDateString(value);
                case ColumnType.Boolean:
                    return GetBooleanString(value);
                default:
                    return value;
            }
        }

        private ColumnDetails GetEdtColumnDetailsFromDisplayName(string displayName)
        {
            var lowerDisplayName = displayName.ToLower();

            var edtColumnDetails = EdtDocumentRepository.GetColumnDetails().ToList();

            var matchedDbName = edtColumnDetails.FirstOrDefault(x => x.DisplayName.ToLower().Equals(lowerDisplayName));

            if (matchedDbName != null)
                return matchedDbName;

            Regex rgx = new Regex("[^a-zA-Z0-9]");
            lowerDisplayName = rgx.Replace(lowerDisplayName, "");

            matchedDbName = edtColumnDetails.Find(x => x.GetAlphaNumbericOnlyDisplayName().ToLower()
                                        .Replace(" ", string.Empty).Equals(lowerDisplayName));

            return matchedDbName ?? throw new Exception($"Unable to determine Edt Db column name from mapped display name {displayName}");
        }

        private static string GetDateString(string sourceDateValue)
        {
            DateTime convertedDate;

            if (sourceDateValue.Length == 14 && (sourceDateValue[0] == '1' || sourceDateValue[0] == '2'))
            {
                convertedDate = DateTime.ParseExact(sourceDateValue, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            }
            else
            {
                if (sourceDateValue.Contains("/") || sourceDateValue.Contains("-") || sourceDateValue.Contains("\\"))
                {
                    convertedDate = DateTime.Parse(sourceDateValue);
                }
                else
                {
                    convertedDate = FromUnixTime(long.Parse(sourceDateValue));
                }
            }
            return convertedDate.ToString("dd/MM/yyyy HH:mm:ss");
        }    

        private static string GetBooleanString(string sourceValue)
        {
            switch (sourceValue.ToLower())
            {
                case "1":
                case "true":
                case "yes":
                    return "True";
                default:
                    return "False";
            }
        }

        private static DateTime FromUnixTime(long unixTime)
        {
            return epoch.AddSeconds(unixTime);
        }

        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
