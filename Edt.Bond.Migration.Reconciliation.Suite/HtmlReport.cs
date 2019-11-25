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
            var path = System.Reflection.Assembly.GetCallingAssembly().CodeBase;
            var actualPath = path.Substring(0, path.LastIndexOf("bin"));

            var projectPath = new Uri(actualPath).LocalPath;
            Directory.CreateDirectory(projectPath.ToString() + "reports");
            var reportPath = projectPath + "reports\\ExtentReport.html";

            var htmlReporter = new ExtentHtmlReporter(reportPath);

            htmlReporter.Config.DocumentTitle = "EDT DM Reconciliation";
            htmlReporter.Config.ReportName = "Idx to EDT Data Migration Reconciliation";
            htmlReporter.Config.EnableTimeline = false;
            htmlReporter.Config.Theme = AventStack.ExtentReports.Reporter.Configuration.Theme.Standard;
            htmlReporter.Config.CSS = CSS;

            AddRunSettingsToReport(htmlReporter);
           
            Writer = new ExtentReports();

            Writer.AttachReporter(htmlReporter);       
        }       
        
        private void AddRunSettingsToReport(ExtentHtmlReporter htmlReporter)
        {            
            htmlReporter.SystemAttributeContext.AddSystemAttribute(new AventStack.ExtentReports.Model.SystemAttribute("Idx Source path", Settings.MicroFocusSourceDirectory));
            htmlReporter.SystemAttributeContext.AddSystemAttribute(new AventStack.ExtentReports.Model.SystemAttribute("Idx name", Settings.IdxName));
            htmlReporter.SystemAttributeContext.AddSystemAttribute(new AventStack.ExtentReports.Model.SystemAttribute("Edt Case Id", Settings.EdtCaseId));
            htmlReporter.SystemAttributeContext.AddSystemAttribute(new AventStack.ExtentReports.Model.SystemAttribute("Edt Importer Dataset name", Settings.EdtImporterDatasetName));
            htmlReporter.SystemAttributeContext.AddSystemAttribute(new AventStack.ExtentReports.Model.SystemAttribute("Edt CFS path", Settings.EdtCfsDirectory));
            htmlReporter.SystemAttributeContext.AddSystemAttribute(new AventStack.ExtentReports.Model.SystemAttribute("Standard Map path", Settings.StandardMapPath));
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            Writer.Flush();
        }

        private static string CSS = @"
            .description { font-style: italic;}
            
            .runtime-table { max-width: 500px; }
            .runtime-table tbody tr:nth-child(1) { background-color: lightgray;} 
        ";
    }
}
