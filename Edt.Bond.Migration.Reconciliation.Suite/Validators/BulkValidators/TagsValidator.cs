using Edt.Bond.Migration.Reconciliation.Framework;
using Edt.Bond.Migration.Reconciliation.Framework.Extensions;
using Edt.Bond.Migration.Reconciliation.Framework.Logging;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using Edt.Bond.Migration.Reconciliation.Framework.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.Validators
{
    public class TagsValidator : BulkValidator, IDisposable
    {
        private readonly string[] _unmigratedTags = new string[] { "Ems Folders:Deleted Items", " LPP/Protected Review" };
        private readonly List<Tag> _workbookRecords;
        private readonly StreamWriter _writer;

        public TagsValidator() : base("Tags", "AUN_WORKBOOK_NUMERIC")
        {
            _workbookRecords = AunWorkbookReader.Read();

            _writer = new StreamWriter(Path.Combine(Settings.ReportingDirectory, "idx_tags.csv"));
            _writer.WriteLine("DocId,Tag");
        }

        public void Validate(List<Document> documents)
        {
            var allEdtTags = EdtDocumentRepository.GetDocumentTags(documents.Select(x => x.DocumentId).ToList());

            documents.ForEach(idxRecord =>
            {
                var aunWorkbookIds = idxRecord.AllFields.Where(x => x.Key.Equals("AUN_WORKBOOK_NUMERIC", StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrWhiteSpace(x.Value) && x.Value != "0").ToList();
                var foundEdtValue = allEdtTags.TryGetValue(idxRecord.DocumentId, out var relatedEdTags);

                var cleanedEdTags = foundEdtValue ? relatedEdTags?.Select(x => x?.ReplaceTagChars()).ToList() : new List<string>();

                if (!aunWorkbookIds.Any())
                {

                    DebugLogger.Instance.WriteLine("aunWorkbookIds");

                    TestResult.IdxNoValue++;
                    TestResult.AddComparisonError(idxRecord.DocumentId,
                        "Warning - AUN_WORKBOOK_NUMERIC field was not present for idx record");

                    if (relatedEdTags != null && relatedEdTags.Any(x => !x.StartsWith("Subjects") && !x.StartsWith("Issues"))) //subject issues
                    {

                        DebugLogger.Instance.WriteLine("aunWorkbookIds2");

                        TestResult.DocumentsInEdtButNotInIdx++;
                        TestResult.AddComparisonError(idxRecord.DocumentId,
                            $"EDT has value(s) {string.Join(",", relatedEdTags)} when Idx record had no value");
                    }
                }
                else
                {
                    
                    var outputTags = aunWorkbookIds
                        .Select(x => _workbookRecords.SingleOrDefault(c => c.Id == x.Value)?.FullPathOutput).ToList();

                    DebugLogger.Instance.WriteLine("outputTags");

                    if (outputTags.Any())
                    {
                        _writer.WriteLine(
                            $"{idxRecord.DocumentId},\"{(outputTags != null ? string.Join(";", outputTags) : string.Empty)}\"");

                        foreach (var aunWorkbookId in aunWorkbookIds)
                        {

                            DebugLogger.Instance.WriteLine("aunWorkbookId2");

                            var tag = _workbookRecords.SingleOrDefault(c => c.Id == aunWorkbookId.Value);

                            if (tag != null)
                            {
                                if (_unmigratedTags.Any(x => tag.FullPath.Contains(x)))
                                {
                                    if (foundEdtValue && relatedEdTags != null && relatedEdTags.Any(x => x != null && x.Equals(tag.FullPath,
                                                                                    System.StringComparison
                                                                                        .InvariantCultureIgnoreCase)))
                                    {
                                        TestResult.Different++;
                                        var edtLogValue = relatedEdTags.Any()
                                            ? string.Join(";", relatedEdTags)
                                            : "none found";

                                        TestResult.ComparisonResults.Add(new Framework.Models.Reporting.ComparisonResult(
                                            idxRecord.DocumentId,
                                            edtLogValue, $"{tag.FullPath} not to be migrated",
                                            aunWorkbookId.Value.ToString()));

                                    }
                                    else
                                    {
                                        TestResult.Matched++;
                                    }
                                }
                                else
                                {
                                    if (!foundEdtValue || (relatedEdTags != null && !relatedEdTags.Any(x =>
                                                               x != null && x.Equals(tag.FullPath,
                                                                   System.StringComparison
                                                                       .InvariantCultureIgnoreCase))))
                                    {
                                        TestResult.Different++;
                                        var edtLogValue = relatedEdTags != null
                                            ? string.Join(";", relatedEdTags)
                                            : "none found";

                                        TestResult.ComparisonResults.Add(new Framework.Models.Reporting.ComparisonResult(
                                            idxRecord.DocumentId,
                                            edtLogValue, tag.FullPath, aunWorkbookId.Value.ToString()));
                                    }
                                    else
                                    {
                                        TestResult.Matched++;
                                    }
                                }
                            }
                            else
                            {
                                TestResult.AddComparisonError(idxRecord.DocumentId,
                                    $"Couldnt convert aun workbook id {aunWorkbookId.Value} to name");
                            }
                        }

                    }
                    else
                    {
                        TestResult.AddComparisonError(idxRecord.DocumentId, "No tags exist for this document");
                    }
                }


            });
        }

        public void Dispose()
        {
            try
            {
                _writer?.Close();
            }
            catch (Exception)
            {
                //Console.WriteLine(e);
                //throw;
            }
        }
    }
}
