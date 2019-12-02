using System;
using System.Globalization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class IdxToEdtConversionService
    {
        public static string ConvertValueToEdtForm(string desiredType, string value)
        {
            switch (desiredType.ToLower())
            {
                case "date":
                    return GetDateString(value);
                case "boolean":
                    return GetBooleanString(value);
                default:
                    return value;
            }
        }

        public static string GetEdtType(string fieldName, string desiredType)
        {
            if(!desiredType.Equals("date", StringComparison.InvariantCultureIgnoreCase) && fieldName.EndsWith("_NUMERICDATE"))
            {
                return "date";
            }

            return desiredType;
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

        public static DateTime FromUnixTime(long unixTime)
        {
            return epoch.AddSeconds(unixTime);
        }
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
