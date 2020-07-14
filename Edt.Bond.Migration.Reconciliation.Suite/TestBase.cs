using AventStack.ExtentReports;
using AventStack.ExtentReports.MarkupUtils;
using Edt.Bond.Migration.Reconciliation.Framework.Logging;
using NUnit.Framework;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Edt.Bond.Migration.Reconciliation.Suite
{
    public class TestBase
    {
        public ExtentTest TestSuite;
        public ExtentTest Test;
        public int TestFailures = 0;
        public int TestTotal = 0;

        [OneTimeSetUp]
        public void SetUpSuiteLog()
        {
            var className = TestContext.CurrentContext.Test.ClassName.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries).Last();
            className = AddSpacesToName(className);
            TestSuite = HtmlReport.Instance.CreateTest(className, TestContext.CurrentContext.Test.Properties.Get("Description")?.ToString());
        }

        [SetUp]
        public void StartLogs()
        {       
            var description = TestContext.CurrentContext.Test.Properties.Get("Description")?.ToString();

            var testName = AddSpacesToName(TestContext.CurrentContext.Test.Name);

            Test = string.IsNullOrEmpty(description) ? TestSuite.CreateNode(testName) : TestSuite.CreateNode(testName, description);

        }        

        [TearDown]
        public void StopLogs()
        {            
            var stackTrace = "<pre>" + TestContext.CurrentContext.Result.StackTrace + "</pre>";
            var errorMessage = MarkupHelper.CreateLabel(TestContext.CurrentContext.Result.Message, ExtentColor.Red);

            if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                Test.AssignCategory("Failed Test");
                Test.Fail(errorMessage);
                Test.Log(Status.Error, stackTrace);
                TestFailures++;               
            }
            else
            {
                Test.Pass(TestContext.CurrentContext.Result.Outcome.Status.ToString());
            }            

            TestTotal++;
        }

        [OneTimeTearDown]
        public void CloseLoggers()
        {
            DebugLogger.Instance.Dispose();
        }


        [OneTimeTearDown]
        public void StopSuiteLog()
        {
            if (TestFailures == 0)
            {
                TestSuite.Pass($"All tests within suite passed ({TestTotal})");
            }
            else
            {
                TestSuite.AssignCategory("Failed Test Suite");
                TestSuite.Fail($"{TestFailures} test(s) failed of {TestTotal}");
            }
        }

        private string AddSpacesToName(string name)
        {
            return Regex.Replace(name, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
        }
    }
}
