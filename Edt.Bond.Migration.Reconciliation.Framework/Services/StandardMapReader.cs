using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

 
            var mappingsLines = File.ReadAllLines(Settings.StandardMapPath);

            if (mappingsLines?.Length == 0)
                throw new Exception("Failed to read standard map");

            ValidateHeadersAreInExpectedPositions(mappingsLines[0]);

            var mappings = new List<StandardMapping>();

            for (var i = 1; i < mappingsLines.Length; i++)
            {
                var newMappingTokens = mappingsLines[i].Split(_delimiter, StringSplitOptions.None);

                var mapping = new StandardMapping(newMappingTokens[EdtFieldNameColumn],
                    newMappingTokens[IdxNameColumn], newMappingTokens[StoredEdtTypeColumn],
                    newMappingTokens[ImporterGroupColumn]);


                if (string.IsNullOrEmpty(mapping.EdtType))
                    TestContext.Out.WriteLine(
                        $"Warning: EDT Field {mapping.EdtName} has no type, thus will NOT be converted in processing.");

                mappings.Add(mapping);

            }

            return DedupeMappings(FilterIgnoredMappings(mappings));
            
        }

        private List<StandardMapping> FilterIgnoredMappings(List<StandardMapping> standardMappings)
        {
            var fieldsToIgnore = Settings.IgnoredIdxFieldsFromComparison.ToList();

            var ignoredMappings = standardMappings.Where(mapping => mapping.EdtName.Equals(mapping.IdxNames.First()) ||
                                                                    fieldsToIgnore.Contains(mapping.IdxNames.First()) ||
                                                                    mapping.EdtName.EndsWith("_unmapped"));

            var standardMappingsList = standardMappings.ToList();

            using (var swIgnored =
                new StreamWriter(Path.Combine(Settings.LogDirectory, "standard_mappings_ignored.csv")))
            {

                ignoredMappings.ToList().ForEach(x =>
                {
                    standardMappingsList.Remove(x);
                    swIgnored.WriteLine($"{x.EdtName},{x.IdxNames}");
                });
            }

            return standardMappingsList;
        }

        private List<StandardMapping> DedupeMappings(IEnumerable<StandardMapping> standardMappings)
        {
            return standardMappings.GroupBy(x => x.EdtName.ToLowerInvariant())
                .Select(x =>
                {
                    var toKeep = x.First();
                    toKeep.IdxNames = x.Select(y => y.IdxNames.First()).ToList();
                    return toKeep;
                })
                .ToList();
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
                throw new Exception(
                    $"Mapping document column {expectedIndex} is not {expectedValue}; found {tokens[expectedIndex]}");
        }
    }
}