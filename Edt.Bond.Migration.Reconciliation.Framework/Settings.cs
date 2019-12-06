using System;
using System.Configuration;
using System.IO;

namespace Edt.Bond.Migration.Reconciliation.Framework
{
    public class Settings
    {
        public static string EdtCaseId => GetSetting("EdtCaseId");

        public static string StandardMapPath => GetSetting("StandardMapPath");

        public static string MicroFocusSourceDirectory => GetSetting("MicroFocusStagingDirectory");

        public static string MicroFocusStagingDirectoryTextPath => GetSetting("MicroFocusStagingDirectoryTextPath");

        public static string MicroFocusStagingDirectoryNativePath => GetSetting("MicroFocusStagingDirectoryNativePath");

        public static string IdxFilePath => GetSetting("IdxFilePath");

        public static string RedactionsFilePath => GetSetting("RedactionsFilePath");

        public static string ConversionToolOutputDirectory => GetSetting("ConversionToolOutputDirectory");

        public static string EdtImporterDatasetName => GetSetting("EdtImporterDataSetName");

        public static string EdtCfsDirectory => GetSetting("EdtCfsDirectory");

        public static int IdxSampleSize => int.Parse(GetSetting("IdxSampleSize"));

        public static string[] IgnoredIdxFieldsFromComparison => GetSetting("IgnoredIdxFieldsFromComparison").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

        public static string[] LocationIdxFields => GetSetting("LocationIdxFields").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

        public static string ReportingDirectory => GetDirectory("Report");

        public static string LogDirectory => GetDirectory("Logs");
        public static string Version => GetSetting("Version");

		public static string EmailFieldIdentifyingPrefix => GetSetting("EmailFieldIdentifyingPrefix");

        private static string GetDirectory(string subfolder = "")
        {
            var assemblyPath = System.Reflection.Assembly.GetCallingAssembly().CodeBase;
            var workingDirectory = Path.GetDirectoryName(new Uri(assemblyPath).LocalPath);

            workingDirectory = string.IsNullOrEmpty(subfolder) ? workingDirectory : Path.Combine(workingDirectory, subfolder);

            if (!Directory.Exists(workingDirectory))
                Directory.CreateDirectory(workingDirectory);

            return workingDirectory;
        }

        private static string GetSetting(string key)
        {
            return ConfigurationManager.AppSettings[key] ??
                throw new Exception($"Setting {key} not found in App.config");
        }
    }
}
