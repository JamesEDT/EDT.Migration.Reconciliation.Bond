using AventStack.ExtentReports;
using AventStack.ExtentReports.Gherkin.Model;
using AventStack.ExtentReports.MarkupUtils;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Reporting;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Edt.Bond.Migration.Reconciliation.Suite
{
    public class TestBase
    {
        public ExtentTest FeatureRunner;
        public bool createdLogHeader;
        public ExtentTest TestLogger;
        public int TestFailures = 0;
        public int TestTotal = 0;
        public List<ComparisonError> ComparisonErrors;
        public List<ComparisonResult> ComparisonResults;

        [OneTimeSetUp]
        public void SetUpSuiteLog()
        {
            var className = TestContext.CurrentContext.Test.ClassName.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries).Last();
            className = AddSpacesToName(className);
            FeatureRunner = HtmlReport.Writer.CreateTest(className, TestContext.CurrentContext.Test.Properties.Get("Description")?.ToString());
        }

        [SetUp]
        public void StartLogs()
        {       
            var description = TestContext.CurrentContext.Test.Properties.Get("Description")?.ToString();

            var testName = AddSpacesToName(TestContext.CurrentContext.Test.Name);

            TestLogger = string.IsNullOrEmpty(description) ? FeatureRunner.CreateNode(testName) : FeatureRunner.CreateNode<Scenario>(testName, description);

            ComparisonErrors = new List<ComparisonError>();
            ComparisonResults = new List<ComparisonResult>();
        }        

        [TearDown]
        public void StopLogs()
        {            
            var status = TestContext.CurrentContext.Result.Outcome.Status;
            var stackTrace = "<pre>" + TestContext.CurrentContext.Result.StackTrace + "</pre>";
            var errorMessage = MarkupHelper.CreateLabel(TestContext.CurrentContext.Result.Message, ExtentColor.Red);

            if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                TestLogger.Fail(errorMessage);
                TestLogger.Log(Status.Error, stackTrace);
                TestFailures++;
            }
            else
            {
                TestLogger.Pass(TestContext.CurrentContext.Result.Outcome.Status.ToString());
            }

            PrintComparisonTables();

            TestTotal++;
        }

        private void PrintComparisonTables()
        {
            //print comparision
            if (ComparisonResults.Count > 0)
            {
                var data = new List<string[]>() { new string[] { "<b>Differences:</b>" }, new string[] { "DocumentId", "Idx value", "Expected Edt value", "Edt value" } };

                data.AddRange(ComparisonResults.Select(x => x.ToTableRow()));

                TestLogger.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data.ToArray()));
            }

            //print errors
            if (ComparisonErrors.Count > 0)
            {
                var data = new List<string[]>() { new string[] { "<b>Errors:</b>"}, new string[] { "DocumentId", "Error message"} };

                data.AddRange(ComparisonErrors.Select(x => x.ToTableRow()));

                TestLogger.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data.ToArray()));
            }
        }

        [OneTimeTearDown]
        public void StopSuiteLog()
        {
            if (TestFailures == 0)
            {
                FeatureRunner.Pass($"All tests within suite passed ({TestTotal})");
            }
            else
            {
                FeatureRunner.Fail($"{TestFailures } test(s) failed of {TestTotal}");
            }
        }

        public void LogMessage(string message)
        {
            TestLogger.Log(Status.Info, message);
        }
        
        public void LogDebugInfo(string message)
        {
            TestLogger.Debug(message);
        }

        private string AddSpacesToName(string name)
        {
            return Regex.Replace(name, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
        }
    }
}
