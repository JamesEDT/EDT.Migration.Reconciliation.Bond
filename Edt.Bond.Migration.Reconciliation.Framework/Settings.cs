using System;
using System.Configuration;
using System.IO;
using Edt.Bond.Migration.Reconciliation.Framework.Extensions;
using NUnit.Framework;

namespace Edt.Bond.Migration.Reconciliation.Framework
{
    public class Settings
    {
        public static bool UseLiteDb => bool.Parse(GetOptionalSetting("UseLiteDb") ?? "false");
        public static bool UseExistingIdxAnalysis => bool.Parse(GetSetting("UseExistingIdxAnalysis"));

        public static string EdtCaseId => GetSetting("EdtCaseId");

        public static string StandardMapPath => GetSetting("CaseMapPath");
        public static string SourceFolderPath => GetSetting("ExtractFolder");

        public static string MfZipLocation => GetSetting("MfZipLocation");

        public static string MicroFocusAunWorkbookPath => GetSetting("AunWorkbookPath");

        public static string MicroFocusStagingDirectoryTextPath => GetSetting("TextPath");

        public static string MicroFocusStagingDirectoryNativePath => GetSetting("NativePath");

        public static string IdxFilePath => GetSetting("IdxFilePath");

        public static string RedactionsFilePath => GetSetting("RedactionsFilePath");

        public static string ConversionToolOutputDirectory => GetSetting("ConversionToolOutputDirectory");

        public static string EdtImporterDatasetName => GetSetting("EdtImporterDataSetName");

        public static string EdtCfsDirectory => GetSetting("EdtCfsDirectory");

        public static int IdxSampleSize => int.Parse(GetSetting("IdxSampleSize"));

        public static string[] AutoPopulatedNullFields => GetSetting("AutoPopulatedIfNull").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

        public static bool GenerateLoadFile => bool.Parse(GetOptionalSetting("GenerateLoadFile") ?? "false");
        
        public static string[] IgnoredIdxFieldsFromComparison => GetSetting("IgnoredIdxFieldsFromComparison").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

        public static string[] LocationIdxFields => GetSetting("LocationIdxFields").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

        public static string ExtractLocation => GetSetting("ExtractFolder");

        public static string ReportingDirectory
        {
            get
            {
                var name = EdtImporterDatasetName.Split("|".ToCharArray()).Length > 1 ? "Multi" : EdtImporterDatasetName;

                if (string.IsNullOrWhiteSpace(_reportingDirectory))
                                    _reportingDirectory = GetDirectory($"ReportV{Version}_Case{EdtCaseId}_{name.TrimNonAlphaNumerics()}_{DateTime.Now.ToString("ddMMyyyy_HHmm")}");

                return _reportingDirectory;
            }
        }
        

        private static string _reportingDirectory;

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

        private static string GetOptionalSetting(string key)
        {
            return ConfigurationManager.AppSettings[key] ??
                   null;
        }
    }
}
