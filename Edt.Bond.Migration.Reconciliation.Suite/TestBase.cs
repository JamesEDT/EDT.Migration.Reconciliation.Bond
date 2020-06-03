using AventStack.ExtentReports;
using AventStack.ExtentReports.Gherkin.Model;
using AventStack.ExtentReports.MarkupUtils;
using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Logging;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Reporting;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Edt.Bond.Migration.Reconciliation.Framework.Output;

namespace Edt.Bond.Migration.Reconciliation.Suite
{
    public class TestBase
    {
        public ExtentTest FeatureRunner;
        public bool createdLogHeader;
        public ExtentTest TestLogger;
        public int TestFailures = 0;
        public int TestTotal = 0;
        public ConcurrentBag<ComparisonError> ComparisonErrors;
        public ConcurrentBag<ComparisonResult> ComparisonResults;

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

            ComparisonErrors = new ConcurrentBag<ComparisonError>();
            ComparisonResults = new ConcurrentBag<ComparisonResult>();
        }        

        [TearDown]
        public void StopLogs()
        {            
            var status = TestContext.CurrentContext.Result.Outcome.Status;
            var stackTrace = "<pre>" + TestContext.CurrentContext.Result.StackTrace + "</pre>";
            var errorMessage = MarkupHelper.CreateLabel(TestContext.CurrentContext.Result.Message, ExtentColor.Red);

            if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                TestLogger.AssignCategory("Failed Test");
                TestLogger.Fail(errorMessage);
                TestLogger.Log(Status.Error, stackTrace);
                TestFailures++;               
            }
            else
            {
                TestLogger.Pass(TestContext.CurrentContext.Result.Outcome.Status.ToString());
            }            

            TestTotal++;
        }

        [OneTimeTearDown]
        public void CloseLoggers()
        {
            DebugLogger.Instance.Dispose();
        }

        public void PrintExpectedOutputFile(string mappingName)
        {
            if (ComparisonResults.Count > 0)
            {
                var filename = Path.Combine(Settings.ReportingDirectory, $"expectedvalues_{mappingName.Replace(" ", string.Empty)}.csv");

                using (var sw = new StreamWriter(filename))
                {
                    sw.WriteLine("DocumentId,Expected Edt value");

                    foreach (var result in ComparisonResults)
                    {
                        sw.WriteLine($"{result.DocumentId},\"{result.IdxConvertedValue}\"");
                    }                
                }
            }
        }

        public string PrintComparisonTables(string mappingName)
        {
            //var filename = Path.Combine(Settings.ReportingDirectory,$"differences_{mappingName.Replace(" ", string.Empty)}.csv");

            //using (var sw = new StreamWriter(filename))
            //{
            //    //print comparision
            //    if (ComparisonResults.Count > 0)
            //    {
            //        sw.WriteLine("Differences:");
            //        sw.WriteLine("DocumentId,Idx value,Expected Edt value,Edt value");

            //        foreach (var result in ComparisonResults) {
            //            sw.WriteLine($"{result.DocumentId},\"{result.IdxValue}\",\"{result.IdxConvertedValue}\",\"{result.EdtValue}\"");                        
            //        }
            //    }

            //    //print errors
            //    if (ComparisonErrors.Count > 0)
            //    {
            //        sw.WriteLine();
            //        sw.WriteLine("Errors/Warnings:");
            //        sw.WriteLine("DocumentId,Error/Warning");
            //        var data = new List<string[]>() { new string[] { "<b>Errors:</b>" }, new string[] { "DocumentId", "Error message" } };

            //        foreach(var error in ComparisonErrors)
            //        {
            //            sw.WriteLine($"{error.DocumentId},{error.ErrorMessage}");
            //        }
            //    }
            //}

            return HtmlDifferencesReport.WriteReport(Settings.ReportingDirectory, mappingName.Replace(" ", string.Empty),
                ComparisonResults, ComparisonErrors);
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
                FeatureRunner.AssignCategory("Failed Test Suite");
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
