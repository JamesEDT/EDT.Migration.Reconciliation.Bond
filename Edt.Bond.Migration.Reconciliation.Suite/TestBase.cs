using AventStack.ExtentReports;
using AventStack.ExtentReports.MarkupUtils;
using NUnit.Framework;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Edt.Bond.Migration.Reconciliation.Suite
{
    public class TestBase
    {
        public ExtentTest FeatureRunner;
        public bool createdLogHeader;
        public ExtentTest TestRunner;
        public int TestFailures = 0;
        public int TestTotal = 0;

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

            TestRunner = string.IsNullOrEmpty(description) ? FeatureRunner.CreateNode(testName) : FeatureRunner.CreateNode(testName, description);
        }        

        [TearDown]
        public void StopLogs()
        {            
            var status = TestContext.CurrentContext.Result.Outcome.Status;
            var stackTrace = "<pre>" + TestContext.CurrentContext.Result.StackTrace + "</pre>";
            var errorMessage = TestContext.CurrentContext.Result.Message;

            if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                var failureMessage = MarkupHelper.CreateLabel(TestContext.CurrentContext.Result.Message, ExtentColor.Red);
                TestRunner.Fail(failureMessage);
                TestRunner.Log(Status.Error, stackTrace);
                TestFailures++;
            }
            else
            {
                TestRunner.Pass("Pass");
            }

            TestTotal++;
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
            TestRunner.Log(Status.Info, message);
        }
        
        public void LogDebugInfo(string message)
        {
            TestRunner.Debug(message);
        }

        private string AddSpacesToName(string name)
        {
            return Regex.Replace(name, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
        }
    }
}
