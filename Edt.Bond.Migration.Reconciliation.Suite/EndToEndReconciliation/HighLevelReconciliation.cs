using AventStack.ExtentReports.MarkupUtils;
using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Logging;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Edt.Bond.Migration.Reconciliation.Framework.Extensions;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
	[Description(
		"Comparing high level counts of documents from the source Idx to targets (Edt database and File store)")]
	public class HighLevelReconciliation : TestBase
	{
		private long _idxDocumentCount;
        private List<string> _idxDocumentIds;

		[OneTimeSetUp]
		public void GetIdxCount()
		{			
            _idxDocumentIds = new IdxDocumentsRepository().GetDocumentIds();
            _idxDocumentCount = _idxDocumentIds.Count;
        }

		[Test]
		[Description(
			"Comparing the count of documents detailed in the Idx compared to the document count found in the Edt database")]
		public void DatabaseDocumentsCountsAreEqualBetweenIdxAndEdtDatabase()
		{
            var EdtDocumentCount = EdtDocumentRepository.GetDocumentCount();

			string[][] data = new string[][]
			{
				new string[] {"Item Evaluated", "Count of Documents"},
				new string[] {"Idx file", _idxDocumentCount.ToString()},
				new string[] {"Edt Database", EdtDocumentCount.ToString()}
			};

			TestLogger.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data));

            if (_idxDocumentCount != EdtDocumentCount)
            {
                PrintOutIdDiffs();
            }

			Assert.AreEqual(_idxDocumentCount, EdtDocumentCount, "File counts should be equal for Idx and Load file");
		}

		private void PrintOutIdDiffs()
		{
			var EdtIds = EdtDocumentRepository.GetDocumentNumbers();

			var edtExceptIdx = EdtIds.Except(_idxDocumentIds);
			var IdxExceptEdt = _idxDocumentIds.Except(EdtIds);



			using (var sw = new StreamWriter($"{Settings.ReportingDirectory}\\DocumentCountDifferences_IdLists.csv"))
			{
				sw.WriteLine("In Edt but not in Idx:");
				foreach (var id in edtExceptIdx)
				{
					sw.WriteLine(id);
				}

				sw.WriteLine("In Idx but not Edt");
				foreach (var id in IdxExceptEdt)
				{
					sw.WriteLine(id);
				}
			}

			TestLogger.Info(
				$"Document Id differences output to <a href=\"DocumentCountDifferences_IdLists.csv\"> DocumentCountDifferences_IdLists.csv</a>");
		}

		[Test]
		[Description(
			"Comparing the count of documents detailed in the Idx to the Edt Central File Store, thus validating all natives are imported to EDT.")]
		public void NativeCountsAreEqualBetweenIdxAndEdtFileStore()
		{
            var cfsDocsForBatch = EdtCfsService.GetDocumentsForBatch();

            var cfsCount = cfsDocsForBatch.Count();

			string[][] data = new string[][]
			{
				new string[] {"Item Evaluated", "Count of Documents"},
				new string[] {"Idx file", _idxDocumentCount.ToString()},
				new string[] {"Edt Central file store", cfsCount.ToString()}
			};

			TestLogger.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data));

            if (_idxDocumentCount != cfsCount)
            {
  
                ListExtensions.DifferencesToFile(cfsDocsForBatch.ToList(), _idxDocumentIds, Path.Combine(Settings.ReportingDirectory, "NativesMissing_InCfsOnly.csv"));
                ListExtensions.DifferencesToFile(_idxDocumentIds, cfsDocsForBatch.ToList(), Path.Combine(Settings.ReportingDirectory,  "NativesMissing_InIdxOnly.csv"));

                TestLogger.Info($"List of Ids without body output to reporting directory (NativeMissing_)");
                
            }

            Assert.AreEqual(_idxDocumentCount, cfsCount, "File counts should be equal for Idx and Load file");
		}

		[Test]
		[Description(
			"Comparing the count of documents detailed in the Idx as LPP documents that have been moved to the quarantine Folder.")]
		public void NativeCountsAreEqualBetweenIdxAndQuarantineFolder()
		{
			int lppDocCount = EdtDocumentRepository.GetDocumentQuarantineDocumentCount();
			int idxLppDocCount = new IdxDocumentsRepository().GetNumberOfLppDocs();

            //DebugLogger.Instance.WriteLine("Determine Counts between idx and quarantine - starting scan files");

            string[][] data =
			{
				new[] {"Item Evaluated", "Count of LPP Documents"},
				new[] {"Idx file", idxLppDocCount.ToString()},
				new[] {"Quarantine Folder", lppDocCount.ToString()}
			};

			TestLogger.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data));

			Assert.AreEqual(lppDocCount, idxLppDocCount,
				"File counts should be equal between IDX and EDT Quarantine folder");
		}


		[Test]
		[Description(
			"Comparing the count of documents detailed in the Redaction Load File and those redacted documents in EDT.")]
		public void RedactedCountsAreEqualBetweenRedactionLoadFile()
		{
			int edtRedactionCount = 0;
			int redactionDocCount = 0;

			if (!string.IsNullOrWhiteSpace(Settings.RedactionsFilePath))
			{
				edtRedactionCount = EdtDocumentRepository.GetDocumentRedactedDocumentCount();
				redactionDocCount = new RedactionLoadFileReader(Settings.RedactionsFilePath).GetRecordCount();
			} 

			string[][] data =
			{
				new[] {"Item Evaluated", "Count of LPP Documents"},
				new[] {"Redaction Load file", redactionDocCount.ToString()},
				new[] {"Document Table", edtRedactionCount.ToString()}
			};

			TestLogger.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data));

			Assert.AreEqual(redactionDocCount, edtRedactionCount,
				"File counts should be equal between Redaction Load files and EDT");
		}

		[Test]
		[Description(
			"Comparing the text document counts in the Idx to the Edt Document.Body, thus validating all text fies are imported to EDT.")]
		public void TextCountsAreEqualBetweenIdxAndEdtFileStore()
		{
            //For each Document in Batch, Count where Body is not null
            var edtDocsWithBody = EdtDocumentRepository.GetDocumentNumbersWithABody();

			//compare against Text count in microfocus dir
			var documentIdsWithinEdt = EdtDocumentRepository.GetDocumentNumbers();

            var microFocusExportDocuments = Directory
                .GetFiles(Settings.MicroFocusStagingDirectoryTextPath, "*.txt", SearchOption.AllDirectories)
                .Select(x => new FileInfo(x))
                .Where(x => x.Length > 3)
                .Select(x => GetDocumentIdFromFilePath(x))
                .Where(x => documentIdsWithinEdt.Contains(x))
                .ToList();

			var mircoFocusDocCount = microFocusExportDocuments.Count();

			//output counts
			string[][] data =
			{
				new[] {"Item Evaluated", "Count of Documents"},
				new[] {"MicroFocus Export text(s)", mircoFocusDocCount.ToString()},
				new[] {"Edt Document.Body", edtDocsWithBody.Count().ToString()}
			};

			TestLogger.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data));

            if (mircoFocusDocCount != edtDocsWithBody.Count())
            {
                ListExtensions.DifferencesToFile(microFocusExportDocuments, edtDocsWithBody, Path.Combine(Settings.ReportingDirectory, "TextContent_MicrofocusOnly.csv"));
                ListExtensions.DifferencesToFile(edtDocsWithBody, microFocusExportDocuments, Path.Combine(Settings.ReportingDirectory, "TextContent_EdtOnly.csv"));

                TestLogger.Info($"List of Ids without body output to reporting directory (TextContent_)");

            }

			Assert.AreEqual(mircoFocusDocCount, edtDocsWithBody.Count(),
				"File counts should be equal for Microfocus load and EDT");
		}

		private string GetDocumentIdFromFilePath(FileInfo fileInfo)
		{
			var fileName = fileInfo.Name.Split(new char[] {'.'}).First();

			return fileName;
		}
	}
}
