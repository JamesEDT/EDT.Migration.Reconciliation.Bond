using Edt.Bond.Migration.Reconciliation.Framework;
using NUnit.Framework;
using System.IO;

namespace Edt.Bond.Migration.Reconciliation.Suite
{
    [SetUpFixture]
    public class RunSettingsLogger
    {
        [OneTimeSetUp]
        public void LogAppConfig()
        {

            using (var sw = new StreamWriter(Path.Combine(Settings.LogDirectory, "settings.csv")))
            {
                sw.WriteLine("edt case id:" + Settings.EdtCaseId);
                sw.WriteLine("edt cfs:" + Settings.EdtCfsDirectory);
                sw.WriteLine("dataset:" + Settings.EdtImporterDatasetName);
                sw.WriteLine("idx path:" + Settings.IdxFilePath);
                sw.WriteLine("sample size:" + Settings.IdxSampleSize);
                sw.WriteLine("aun workbook path:" + Settings.MicroFocusAunWorkbookPath);
                sw.WriteLine("standard map path:" + Settings.StandardMapPath);
            }
        }
    }
}
