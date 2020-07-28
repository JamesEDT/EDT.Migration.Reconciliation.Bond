using Edt.Bond.Migration.Reconciliation.Framework.Logging;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Exceptions;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class IdxToEdtConversionService
    {
        public StandardMapping _standardMapping;
        private ColumnDetails _edtColumnDetails;
        private bool _ignoreMissingEdtColumn = false;

        public string MappedEdtDatabaseColumn => _edtColumnDetails?.ColumnName;

        public ColumnDetails EdtColumnDetails => _edtColumnDetails;

        public ColumnType? MappedEdtDatabaseColumnType => _edtColumnDetails?.DataType;

        private static Dictionary<string, string> _timeZones = new Dictionary<string, string>() {
            {"ACDT", "+1030"},
            {"ACST", "+0930"},
            {"ADT", "-0300"},
            {"AEDT", "+1100"},
            {"AEST", "+1000"},
            {"AHDT", "-0900"},
            {"AHST", "-1000"},
            {"AST", "-0400"},
            {"AT", "-0200"},
            {"AWDT", "+0900"},
            {"AWST", "+0800"},
            {"BAT", "+0300"},
            {"BDST", "+0200"},
            {"BET", "-1100"},
            {"BST", "-0300"},
            {"BT", "+0300"},
            {"BZT2", "-0300"},
            {"CADT", "+1030"},
            {"CAST", "+0930"},
            {"CAT", "-1000"},
            {"CCT", "+0800"},
            {"CDT", "-0500"},
            {"CED", "+0200"},
            {"CET", "+0100"},
            {"CEST", "+0200"},
            {"CST", "-0600"},
            {"EAST", "+1000"},
            {"EDT", "-0400"},
            {"EED", "+0300"},
            {"EET", "+0200"},
            {"EEST", "+0300"},
            {"EST", "-0500"},
            {"FST", "+0200"},
            {"FWT", "+0100"},
            {"GMT", "GMT"},
            {"GST", "+1000"},
            {"HDT", "-0900"},
            {"HST", "-1000"},
            {"IDLE", "+1200"},
            {"IDLW", "-1200"},
            {"IST", "+0530"},
            {"IT", "+0330"},
            {"JST", "+0900"},
            {"JT", "+0700"},
            {"MDT", "-0600"},
            {"MED", "+0200"},
            {"MET", "+0100"},
            {"MEST", "+0200"},
            {"MEWT", "+0100"},
            {"MST", "-0700"},
            {"MT", "+0800"},
            {"NDT", "-0230"},
            {"NFT", "-0330"},
            {"NT", "-1100"},
            {"NST", "+0630"},
            {"NZ", "+1100"},
            {"NZST", "+1200"},
            {"NZDT", "+1300"},
            {"NZT", "+1200"},
            {"PDT", "-0700"},
            {"PST", "-0800"},
            {"ROK", "+0900"},
            {"SAD", "+1000"},
            {"SAST", "+0900"},
            {"SAT", "+0900"},
            {"SDT", "+1000"},
            {"SST", "+0200"},
            {"SWT", "+0100"},
            {"USZ3", "+0400"},
            {"USZ4", "+0500"},
            {"USZ5", "+0600"},
            {"USZ6", "+0700"},
            {"UT", "-0000"},
            {"UTC", "-0000"},
            {"UZ10", "+1100"},
            {"WAT", "-0100"},
            {"WET", "-0000"},
            {"WST", "+0800"},
            {"YDT", "-0800"},
            {"YST", "-0900"},
            {"ZP4", "+0400"},
            {"ZP5", "+0500"},
            {"ZP6", "+0600"}
        };

        public IdxToEdtConversionService(StandardMapping standardMapping, bool ignoreMissingEdtColumn = false)
        {
            _standardMapping = standardMapping;

            if (!_standardMapping.IsPartyField())
            {
                try
                {
                    _edtColumnDetails = GetEdtColumnDetailsFromDisplayName(standardMapping.EdtName);
                }
                catch (Exception)
                {
                }
            }

            _ignoreMissingEdtColumn = ignoreMissingEdtColumn;
        }

        public string ConvertValueToEdtForm(string value)
        {
            try
            {
                if (_edtColumnDetails?.DataType == ColumnType.Date || (!string.IsNullOrWhiteSpace(_standardMapping.EdtType) && _standardMapping.EdtType.Equals("Date", StringComparison.InvariantCultureIgnoreCase))
                    || _standardMapping.EdtName.ToLower().Contains("date"))
                {
                    try
                    {
                        return GetDateString(value, _edtColumnDetails?.DataType != ColumnType.Date);
                    }
                    catch(Exception)
                    {
                        if (_standardMapping.EdtType.Equals("date", StringComparison.InvariantCultureIgnoreCase))
                        {
                            return string.Empty;
                        }
                        else
                        {
                            return value;
                        }
                    }
                }

                if (_edtColumnDetails?.DataType == ColumnType.Boolean)
                {
                    try
                    {
                        return GetBooleanString(value);
                    }
                    catch(Exception)
                    {
                        return string.Empty;
                    }
                }
            }
            catch (Exception e)
            {
                DebugLogger.Instance.WriteException(e, $"Convert value {value} to {_edtColumnDetails?.DataType}");
            }

            //trim string
            if (_edtColumnDetails != null && _edtColumnDetails.Size.HasValue && _edtColumnDetails.Size > 0 && value.Length > _edtColumnDetails.Size)
            {
                return value.Substring(0, _edtColumnDetails.Size.Value);
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

            var matchedDb = matchedDbNames?.FirstOrDefault();

            if (!_ignoreMissingEdtColumn && matchedDb == null)
            {
                throw new EdtColumnException($"Unable to determine Edt Db column name from mapped display name {displayName}");
            }
            return matchedDb;
        }

        private static string ReplaceTimeZone(string source)
        {
            var matchedTimezone = _timeZones.FirstOrDefault(x => source.Contains(x.Key));

            return matchedTimezone.Key != null ? source.Replace(matchedTimezone.Key, matchedTimezone.Value) : source;

        }

        private static string GetDateString(string sourceDateValue, bool isText = false)
        {
            DateTime convertedDate;
            DateTimeOffset convertedDateOffset;
            var outputFormat = isText ? "dd/MM/yyyy HH:mm:ss" : "M/d/yyyy h:mm:ss tt";

            string[] formats = new string[] {
                                 "yyyyMMddHHmmss",
                                 "yyyy.MM.dd.HH.mm.ss",
                                 "yyyy:MM:dd HH:mm:ss",
                                 @"ddd, dd MMM yyyy HH:mm:ss zzz",
                                 @"ddd, dd MMM yyyy HH:mm:ss zzz +0000",
                                 @"yyyy-MM-dd HH:mm:ss Z",
                                 @"yyyy-MM-dd HH:mm:ss zzz",
                                 @"ddd, dd MMM yyyy HH:mm:ss Z",
                                 "dd/MM/yyyy HH:mm",
            };


            if (!sourceDateValue.Trim().Substring(1).All(char.IsNumber) || (sourceDateValue.Length == 14 && (sourceDateValue[0] == '1' || sourceDateValue[0] == '2')))
            {
                if (sourceDateValue.Contains("("))
                    sourceDateValue = sourceDateValue.Substring(0, sourceDateValue.LastIndexOf('('));

                if (sourceDateValue.Contains(";"))
                    sourceDateValue = sourceDateValue.Replace(";", string.Empty);

                sourceDateValue = ReplaceTimeZone(sourceDateValue);
                var success = DateTimeOffset.TryParseExact(sourceDateValue, formats, CultureInfo.InvariantCulture.DateTimeFormat,

                                    DateTimeStyles.AllowWhiteSpaces, out convertedDateOffset);

                if (!success)
                {
                    success = DateTimeOffset.TryParse(sourceDateValue, out convertedDateOffset);
                }
                if (success)
                {
                    return convertedDateOffset.UtcDateTime.ToString("dd/MM/yyyy HH:mm:ss");
                }
                else
                {
                    throw new Exception($"Unsupported Format {sourceDateValue}");
                }
            }
            else
            {
                try
                {
                    convertedDate = (sourceDateValue.Length <= 10 ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(sourceDateValue)) : DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(sourceDateValue))).UtcDateTime;
                    if (convertedDate.Year == 1970)
                        throw new ArgumentOutOfRangeException("Suspected Epoch milliseconds");

                }
                catch (ArgumentOutOfRangeException)
                {
                    convertedDate = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(sourceDateValue)).UtcDateTime;
                    if (convertedDate.Year == 1970)
                    {
                        throw new Exception($"Epoch Date Conversion of {sourceDateValue} failed");
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Epoch Date Conversion of {sourceDateValue}", e);
                }
            }

            return convertedDate.ToString(outputFormat);
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

        public string TrimJoinedString(string fullString)
        {
            if (_edtColumnDetails != null && _edtColumnDetails.Size.HasValue && _edtColumnDetails.Size > 0 && fullString.Length > _edtColumnDetails.Size)
            {
                return fullString.Substring(0, _edtColumnDetails.Size.Value);
            }

            return fullString;
        }
    }
}
