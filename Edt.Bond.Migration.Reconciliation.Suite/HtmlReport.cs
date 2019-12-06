using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using Edt.Bond.Migration.Reconciliation.Framework;
using NUnit.Framework;
using System;
using System.IO;

namespace Edt.Bond.Migration.Reconciliation.Suite
{
    [SetUpFixture]
    public class HtmlReport
    {
        public static ExtentReports Writer;       

        [OneTimeSetUp]
        public void ReportSetup()
        {
            var reportFolder = Path.Combine(Settings.ReportingDirectory, "index.html");

            var htmlReporter = new ExtentHtmlReporter(reportFolder);

            htmlReporter.Config.DocumentTitle = "EDT Data Migration Report";
            htmlReporter.Config.ReportName = $"Data Migration Validation Report (v{Settings.Version}) - Edt Case {Settings.EdtCaseId} Batch Name {Settings.EdtImporterDatasetName}";
            htmlReporter.Config.EnableTimeline = false;
            htmlReporter.Config.Theme = AventStack.ExtentReports.Reporter.Configuration.Theme.Standard;
            htmlReporter.Config.CSS = CSS;
            
            Writer = new ExtentReports();

            Writer.AttachReporter(htmlReporter);
        }

        private void RenameOldFolders()
        {
            var renameSuffix = "_old" + DateTime.Now.Ticks;

            if (Directory.Exists(Settings.ReportingDirectory))
                Directory.Move(Settings.ReportingDirectory, Settings.ReportingDirectory + renameSuffix);           
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            Writer.Flush();
        }

        private static string CSS = @"
            .description { font-style: italic;}            
            .runtime-table { max-width: 500px; }
            .runtime-table tbody tr:nth-child(2) { background-color: lightgray;} 
        ";
    }
}
