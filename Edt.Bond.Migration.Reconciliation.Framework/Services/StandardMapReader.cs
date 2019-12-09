using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using NUnit.Framework;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class StandardMapReader
    {
        private const int EdtFieldNameColumn = 0;
        private const int IdxNameColumn = 1;
        private const int StoredEdtTypeColumn = 5;
        private const int ImporterGroupColumn = 6;
        private readonly string[] _delimiter = new string[] {","};


        public IEnumerable<StandardMapping> GetStandardMappings()
        {
            var fieldsToIgnore = Settings.IgnoredIdxFieldsFromComparison.ToList();

            using (var sr = new StreamReader(Settings.StandardMapPath))
            {
                using (var swIgnored = new StreamWriter(Path.Combine(Settings.LogDirectory, "standard_mappings_ignored.csv")))
                {
                    var headers = sr.ReadLine();
                    ValidateHeadersAreInExpectedPositions(headers);

                    var mappings = new List<StandardMapping>();

                    while (!sr.EndOfStream)
                    {
                        var newMappingTokens = sr.ReadLine()?.Split(_delimiter, StringSplitOptions.None);

                        if (newMappingTokens == null) continue;

                        var mapping = new StandardMapping()
                        {
                            EdtName = newMappingTokens[EdtFieldNameColumn],
                            IdxName = newMappingTokens[IdxNameColumn],
                            EdtType = newMappingTokens[StoredEdtTypeColumn],
                            ImportGroup = newMappingTokens[ImporterGroupColumn]
                        };

                        if (mapping.EdtName.Equals(mapping.IdxName) || fieldsToIgnore.Contains(mapping.IdxName) || mapping.EdtName.EndsWith("_unmapped"))
                        {
                            swIgnored.WriteLine($"{mapping.EdtName},{mapping.IdxName}");
                        }
                        else
                        {

                            if (string.IsNullOrEmpty(mapping.EdtType))
                                TestContext.Out.WriteLine(
                                    $"Warning: EDT Field {mapping.EdtName} has no type, thus will NOT be converted in processing.");

                            mappings.Add(mapping);
                        }
                    }

                    if (mappings.Count <= 0)
                        throw new Exception("Failed to read mappings from standard mapping doc, count is 0");

                    return mappings;
                }
            }
        }

        private void ValidateHeadersAreInExpectedPositions(string headerLine)
        {
            var headerTokens = headerLine.Split(_delimiter, StringSplitOptions.None);

            ValidateHeaderValue("Field Name in EDT", EdtFieldNameColumn, headerTokens);
            ValidateHeaderValue("IDOL Name (in IDX file)", IdxNameColumn, headerTokens);
            ValidateHeaderValue("Stored in EDT as", StoredEdtTypeColumn, headerTokens);
            ValidateHeaderValue("Importer Group", ImporterGroupColumn, headerTokens);
        }

        private void ValidateHeaderValue(string expectedValue, int expectedIndex, string[] tokens)
        {
            if (!tokens[expectedIndex].Equals(expectedValue))
                throw new Exception($"Mapping document column {expectedIndex} is not {expectedValue}; found {tokens[expectedIndex]}");
        }

    }
}
