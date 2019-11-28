using System;
using System.Configuration;

namespace Edt.Bond.Migration.Reconciliation.Framework
{
    public class Settings
    {
        public static string EdtCaseId => GetSetting("EdtCaseId");

        public static string StandardMapPath => GetSetting("StandardMapPath");

        public static string MicroFocusSourceDirectory => GetSetting("MicroFocusStagingDirectory");

        public static string IdxFilePath => GetSetting("IdxFilePath");

        public static string ConversionToolOutputDirectory => GetSetting("ConversionToolOutputDirectory");

        public static string EdtImporterDatasetName => GetSetting("EdtImporterDataSetName");

        public static string EdtCfsDirectory => GetSetting("EdtCfsDirectory");

        public static int IdxSampleSize => int.Parse(GetSetting("IdxSampleSize"));

        private static string GetSetting(string key)
        {
            return ConfigurationManager.AppSettings[key] ??
                throw new Exception($"Setting {key} not found in App.config");
        }
    }
}
