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
        public static ExtentReports Instance;       

        [OneTimeSetUp]
        public void ReportSetup()
        {
            var reportFolder = Path.Combine(Settings.ReportingDirectory, "index.html");
            
            var htmlReporter = new ExtentHtmlReporter(reportFolder);

            htmlReporter.Config.DocumentTitle = "EDT Data Migration Report";
            htmlReporter.Config.ReportName = $"Data Migration Validation Report (v{Settings.Version}) - Edt Case {Settings.EdtCaseId} Batch Name {Settings.EdtImporterDatasetName}";
            htmlReporter.Config.EnableTimeline = false;
            htmlReporter.Config.Theme = AventStack.ExtentReports.Reporter.Configuration.Theme.Standard;
            htmlReporter.Config.CSS = Css;
            
            Instance = new ExtentReports();

            Instance.AttachReporter(htmlReporter);
        }       

        [OneTimeTearDown]
        public void Teardown()
        {
            Instance.Flush();
        }

        private const string Css = @"
            .description { font-style: italic;}            
            .runtime-table { max-width: 500px; }
            .runtime-table tbody { width: 500px; }
            .runtime-table tbody td { min-width: 50px; }
            .runtime-table tbody .table-header { background-color: lightgray;} 
        ";
    }
}
