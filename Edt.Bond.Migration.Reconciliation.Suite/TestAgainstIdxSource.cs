using AventStack.ExtentReports.Gherkin.Model;
using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite
{
    [SetUpFixture]
    public class TestAgainstIdxSource
    {
        public static IEnumerable<StandardMapping> StandardMappings;
        public static IdxDocumentsRepository IdxDocumentsRepository;

        public static long IdxDocumentsProcessed = 0;

        [OneTimeSetUp]
        public void AnalyseIdx()
        {
            var logger = HtmlReport.Writer.CreateTest("Validation Setup", "Processing Idx into local store for reconciliation tests against IdX");

            try
            {
                logger.Debug("Analysing Standard Mappings");

                StandardMappings = new StandardMapReader().GetStandardMappings().Where(x => !string.IsNullOrEmpty(x.EdtName) && x.IdxNames.Any(y => !string.IsNullOrWhiteSpace(y)));

                if (!StandardMappings.Any())
                    throw new Exception("Failed to read mappings - count is 0 post attempt");


                if (Settings.UseExistingIdxAnalysis)
                {
                    logger.Debug("Using existing Idx analysis");
                    if(!IdxDocumentsRepository.Exists())
                    {
                        throw new Exception("Idx analysis db not present but config is to use existing analysis");
                    }
                }
                else
                {

                    var idxPath = Settings.IdxFilePath;

                    Document[] documents;

                    IdxDocumentsRepository = new IdxDocumentsRepository();
                    IdxDocumentsRepository.Initialise(true);

                    logger.Debug("Reading Idx chunks");
                    do
                    {
                        documents = IdxProcessingService.GetNextDocumentChunkFromFile(idxPath).ToArray();

                        if (!documents.Any()) return;

                        IdxDocumentsRepository.AddDocuments(documents);
                        IdxDocumentsProcessed = +documents.Length;

                    } while (documents.Length > 0);

                    logger.Debug("Completed reading Idx");

                    IdxDocumentsRepository.CreateDocumentIdIndex();

                    logger.Pass("Idx pre reading completed");
                }
                HtmlReport.Writer.Flush();
                
            }
            catch(Exception ex)
            {
                logger.Log(AventStack.ExtentReports.Status.Info, ex.Message);
                logger.Log(AventStack.ExtentReports.Status.Info, ex.StackTrace);

                if(ex.InnerException != null)
                {
                    logger.Log(AventStack.ExtentReports.Status.Info, ex.InnerException.Message);
                    logger.Log(AventStack.ExtentReports.Status.Info, ex.InnerException.StackTrace);

                }
                logger.Fail("Failed to analyse Idx");
                HtmlReport.Writer.Flush();
                throw ex;
            }
        }
    }
}
