using AventStack.ExtentReports.MarkupUtils;
using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Extensions;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using MoreLinq;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.EndToEndReconciliation
{
    [TestFixture]
	[Category("HighLevel")]
	[Description(
		"Comparing high level counts of documents from the source Idx to targets (Edt database and File store)")]
	//[Parallelizable(ParallelScope.All)]
	public class HighLevelReconciliation : TestBase
	{
		private long _idxDocumentCount;
        private List<string> _idxDocumentIds;
		private Dictionary<string, string> _extractContents;

		[OneTimeSetUp]
		public void GetIdxCount()
		{
			//var cfsDocsForBatch = EdtCfsService.GetDocumentsForBatch();

			_idxDocumentIds =  Settings.UseLiteDb ? new IdxDocumentsRepository().GetDocumentIds() : new IdxReaderByChunk(File.OpenText(Settings.IdxFilePath)).GetDocumentIds();
            _idxDocumentCount = _idxDocumentIds.Count;
			_extractContents = new _7ZipService().GetFiles(Settings.MfZipLocation);
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

			Test.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data));

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

			Test.Info(
				$"Document Id differences output to <a href=\"DocumentCountDifferences_IdLists.csv\"> DocumentCountDifferences_IdLists.csv</a>");
		}

		//[Test]
		//[Ignore("Too Slow")]
		//[Description(
		//	"Comparing the count of documents detailed in the Idx to the Edt Central File Store, thus validating all natives are imported to EDT.")]
		//public void NativeCountsAreEqualBetweenIdxAndEdtFileStore()
		//{			

		//	var cfsDocsForBatch = EdtCfsService.GetDocumentsForBatch();
  //          var cfsCount = cfsDocsForBatch.Count();

		//	var nativesInZipAndIdx = _idxDocumentIds.Where(x => _extractContents.ContainsKey(x)).ToList();

		//	string[][] data = new string[][]
		//	{
		//		new string[] {"Item Evaluated", "Count of Documents"},
		//		new string[] {"Idx file", _idxDocumentCount.ToString()},
		//		new string[] {"Natives In Zip Extract For Idx docs", nativesInZipAndIdx.Count.ToString()},
		//		new string[] {"Edt Central file store", cfsCount.ToString()}
		//	};

		//	Test.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data));

  //          if (nativesInZipAndIdx.Count != cfsCount)
  //          {
  
  //              ListExtensions.DifferencesToFile(cfsDocsForBatch.ToList(), nativesInZipAndIdx, Path.Combine(Settings.ReportingDirectory, "NativesMissing_InCfsOnly.csv"));
  //              ListExtensions.DifferencesToFile(nativesInZipAndIdx, cfsDocsForBatch.ToList(), Path.Combine(Settings.ReportingDirectory,  "NativesMissing_InIdxOnly.csv"));

  //              Test.Info($"List of Ids without body output to reporting directory (NativeMissing_)");
                
  //          }

  //          Assert.AreEqual(nativesInZipAndIdx.Count, cfsCount, "File counts should be equal for Idx/Zip Content vs Load file");
		//}

		[Test]
		[Description(
			"Comparing the count of documents detailed in the Idx to the Edt Central File Store, thus validating all natives are imported to EDT.")]
		public void NativeCountsAreEqualBetweenIdxAndEdtFileStore()
		{
			var nativesInZipAndIdx = _idxDocumentIds.Where(x => _extractContents.ContainsKey(x)).ToList();

			var missing = new ConcurrentBag<string>();

			var edtDocumentIds = EdtDocumentRepository.GetAllDocumentIds().ToDictionary(x => (string) x.DocumentNumber, x => (int) x.DocumentId);

			nativesInZipAndIdx
				.AsParallel().ForEach(native =>
				{
					var edtId = edtDocumentIds[native];

					if (!NativeExtension.IsNativePresent(edtId))
					{
						missing.Add(native);
					}
				});

			

			ListExtensions.WriteToFile(missing.ToList(), Path.Combine(Settings.ReportingDirectory,"MissingNativeFromCFS.csv"));

			string[][] data = new string[][]
			{
				new string[] {"Item Evaluated", "Count of Documents"},
				new string[] {"Idx file", _idxDocumentCount.ToString()},
				new string[] {"Natives In Zip Extract For Idx docs", nativesInZipAndIdx.Count.ToString()},
				new string[] {"Edt Central file store", (nativesInZipAndIdx.Count - missing.Count()).ToString()}
			};

			Test.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data));

			Assert.Zero(missing.Count(), "Missing natives should be 0");
		}


		[Test]
		[Description(
			"Comparing the count of documents detailed in the Idx as LPP documents that have been moved to the quarantine Folder.")]
		public void NativeCountsAreEqualBetweenIdxAndQuarantineFolder()
		{
			if (Settings.MicroFocusStagingDirectoryNativePath.ToUpper().Contains("NON-LPP") ||
				Settings.MicroFocusStagingDirectoryNativePath.ToUpper().Contains("NONLPP"))
			{
				Assert.Pass("Not Required for NON-LPP data");
			}
			else
			{
				int lppDocCount = EdtDocumentRepository.GetDocumentQuarantineDocumentCount();
				int idxLppDocCount = Settings.UseLiteDb ? new IdxDocumentsRepository().GetNumberOfLppDocs() : new IdxReaderByChunk(File.OpenText(Settings.IdxFilePath)).GetDocumentIds().Count;

				//DebugLogger.Instance.WriteLine("Determine Counts between idx and quarantine - starting scan files");

				string[][] data =
				{
				new[] {"Item Evaluated", "Count of LPP Documents"},
				new[] {"Idx file", idxLppDocCount.ToString()},
				new[] {"Quarantine Folder", lppDocCount.ToString()}
			};

				Test.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data));

				Assert.AreEqual(lppDocCount, idxLppDocCount,
					"File counts should be equal between IDX and EDT Quarantine folder");
			}
		}


		//natie count test

		[Test]
		[Ignore("Not needed")]
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

			Test.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data));

			Assert.AreEqual(redactionDocCount, edtRedactionCount,
				"File counts should be equal between Redaction Load files and EDT");
		}

		[Test]
		[Description(
			"Comparing the text document counts in the Idx to the Edt Document.Body, thus validating all text fies are imported to EDT.")]
		public void TextCountsAreEqualBetweenIdxAndEdtFileStore()
		{
			//zip text files
			var emsZipTextFiles = new _7ZipService().GetFiles(Settings.MfZipLocation, false);

			//For each Document in Batch, Count where Body is not null
			var edtDocsWithBody = EdtDocumentRepository.GetDocumentNumbersWithABody();
			
			
			//filter empty to give idx only ids
			var missingIdxTexts = _idxDocumentIds.Except(edtDocsWithBody);

			var missingIdxWithSize = missingIdxTexts.Where(x => emsZipTextFiles.ContainsKey(x) 
				&& File.Exists(Path.Combine(Settings.ExtractLocation, emsZipTextFiles[x]))
				&& new FileInfo(Path.Combine(Settings.ExtractLocation,emsZipTextFiles[x]))?.Length > 3).ToList();


			//output counts
			string[][] data =
			{
				new[] {"Item Evaluated", "Count of Documents"},
				new[] {"MicroFocus Export text(s)", (_idxDocumentIds.Count() - missingIdxTexts.Count() + missingIdxWithSize.Count()).ToString()},
				new[] {"Edt Document.Body", edtDocsWithBody.Count().ToString()}
			};

			Test.Log(AventStack.ExtentReports.Status.Info, MarkupHelper.CreateTable(data));

			if (missingIdxWithSize.Any())
			{
				ListExtensions.WriteToFile(missingIdxWithSize, Path.Combine(Settings.ReportingDirectory, "TextContent_MicrofocusOnly.csv"));

				Test.Info($"List of Ids without body output to reporting directory (TextContent_)");
			}

			Assert.Zero(missingIdxWithSize.Count, "Missing text content identified");
		}

		private string GetDocumentIdFromFilePath(FileInfo fileInfo)
		{
			var fileName = fileInfo.Name.Split(new char[] {'.'}).First();

			return fileName;
		}

		private string GetDocumentIdFromFilePath(string filePath)
		{
			var fileName = Path.GetFileNameWithoutExtension(filePath);

			return fileName;
		}
	}
}
