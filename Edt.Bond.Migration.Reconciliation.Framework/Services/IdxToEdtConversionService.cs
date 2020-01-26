using Edt.Bond.Migration.Reconciliation.Framework.Logging;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Exceptions;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class IdxToEdtConversionService
    {
        public StandardMapping _standardMapping;
        private ColumnDetails _edtColumnDetails;

        public string MappedEdtDatabaseColumn => _edtColumnDetails?.ColumnName;

        public ColumnDetails EdtColumnDetails => _edtColumnDetails;

        public ColumnType? MappedEdtDatabaseColumnType => _edtColumnDetails?.DataType;
       
        public IdxToEdtConversionService(StandardMapping standardMapping)
        {
            _standardMapping = standardMapping;

            if (!_standardMapping.IsEmailField())
                _edtColumnDetails = GetEdtColumnDetailsFromDisplayName(standardMapping.EdtName);
        }

        public string ConvertValueToEdtForm(string value)
        {
            try
            {
                if (_edtColumnDetails?.DataType == ColumnType.Date || (!string.IsNullOrWhiteSpace(_standardMapping.EdtType) && _standardMapping.EdtType.Equals("Date", StringComparison.InvariantCultureIgnoreCase)))
                {
                    return GetDateString(value);
                }

                if (_edtColumnDetails?.DataType == ColumnType.Boolean)
                    return GetBooleanString(value);
            }
            catch (Exception e)
            {
                DebugLogger.Instance.WriteException(e, $"Convert value {value} to {_edtColumnDetails?.DataType}");
            }
            
            return value;
        }

        private ColumnDetails GetEdtColumnDetailsFromDisplayName(string displayName)
        {
            if (displayName.Equals("host document id", StringComparison.InvariantCultureIgnoreCase))
                return new ColumnDetails()
                {
                    ColumnName = "ImportedParentNumber",
                    DataType = ColumnType.Text
                };

            var lowerDisplayName = displayName.ToLower();

            var edtColumnDetails = EdtDocumentRepository.GetColumnDetails().ToList();

            var matchedDbNames = edtColumnDetails.Where(x => x.DisplayName.ToLower().Equals(lowerDisplayName) || x.ExportDisplayName.ToLower().Equals(lowerDisplayName));

            if (matchedDbNames != null && matchedDbNames.Count() > 0)
                return matchedDbNames.First();

            Regex rgx = new Regex("[^a-zA-Z0-9]");
            lowerDisplayName = rgx.Replace(lowerDisplayName, "");

            matchedDbNames = edtColumnDetails.FindAll(x => x.GetAlphaNumbericOnlyDisplayName().ToLower()
                                        .Replace(" ", string.Empty).Equals(lowerDisplayName));

            return matchedDbNames?.FirstOrDefault();
            //?? throw new EdtColumnException($"Unable to determine Edt Db column name from mapped display name {displayName}");
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
                    try
                    {
                        convertedDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(sourceDateValue)).UtcDateTime;

                        //convertedDate = FromUnixTime(long.Parse(sourceDateValue));
                    }
                    catch (Exception e)
                    {
                        DebugLogger.Instance.WriteException(e, $"Epoch Date Conversion of {sourceDateValue}");
                        throw e;
                    }
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
