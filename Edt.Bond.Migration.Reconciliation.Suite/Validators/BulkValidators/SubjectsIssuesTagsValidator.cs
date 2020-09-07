using Edt.Bond.Migration.Reconciliation.Framework.Extensions;
using Edt.Bond.Migration.Reconciliation.Framework.Logging;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using Edt.Bond.Migration.Reconciliation.Framework.Repositories;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Suite.Validators
{
    public class SubjectIssuesTagsValidator : BulkValidator, IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly List<string> _subjectFields = new List<string>() { "TAG_SUBJECTS_MATCH_PARAMETRIC", "TAG_SUBJECTS_MATCH" };
        private readonly List<string> _issueFields = new List<string>() { "TAG_ISSUES_MATCH_PARAMETRIC", "TAG_ISSUES_MATCH" };

        public SubjectIssuesTagsValidator() : base("Subjects/Issues Tags", "TAG_SUBJECTS_MATCH_PARAMETRIC/TAG_ISSUES_MATCH_PARAMETRIC")
        {
        }

        public void Validate(List<Document> documents)
        {
            var allEdtTags = EdtDocumentRepository.GetDocumentTags(documents.Select(x => x.DocumentId).ToList());

            documents.ForEach(idxRecord =>
            {
                //get subject or issue t
                var subjects = idxRecord.GetValuesForIdolFields(_subjectFields);
                var issues = idxRecord.GetValuesForIdolFields(_issueFields);

                var expectedTags = subjects.Select(x => $"Subjects:{x}")
                .Union(issues.Select(x => $"Issues:{x}"));

                var foundEdtValue = allEdtTags.TryGetValue(idxRecord.DocumentId, out var relatedEdTags);

                var mvTags = foundEdtValue ? relatedEdTags?.Where(x => x.StartsWith("Issues:") || x.StartsWith("Subjects:")).ToList() : new List<string>();

                if (!expectedTags.Any())
                {

                    TestResult.IdxNoValue++;
                    TestResult.Matched++;
                }
                else
                {

                    foreach (var expected in expectedTags)
                    {
                        if (!mvTags.Any(x => x.Equals(expected, StringComparison.InvariantCultureIgnoreCase)))
                        {  

                            TestResult.Different++;
                            var edtLogValue = mvTags.Any()
                                ? string.Join(";", mvTags)
                                : "none found";

                            TestResult.ComparisonResults.Add(new Framework.Models.Reporting.ComparisonResult(
                                idxRecord.DocumentId,
                                edtLogValue, expected, expected));

                        }
                        else
                        {
                            TestResult.Matched++;
                        } 
                        
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
