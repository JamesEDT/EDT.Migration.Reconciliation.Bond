using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase;
using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase.Dto;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Framework.Repositories
{
    public class EdtDocumentRepository
    {
        private const string EdtConnectionStringConfigKey = "EdtDatabaseConnectionString";
        private static SqlExecutor _sqlExecutor;

        public static SqlExecutor SqlExecutor => _sqlExecutor ?? (_sqlExecutor = new SqlExecutor(GetConnectionStringByName()));

        public static IEnumerable<ColumnDetails> GetAllCustomFieldDetails(string caseId)
        {
            return GetColumnDetails(caseId).Where(x => x.IsCustomColumn != null && (x.ColumnName.StartsWith("cc_") || x.IsCustomColumn.Value));
        }

        public static IEnumerable<IDictionary<string, object>> GetDocuments(List<string> documentIds)
        {
            var sql = $"SELECT * FROM {GetDatabaseName()}.[Document] WHERE DocNumber in @documentIds";
            return SqlExecutor.Query(sql, new { documentIds }).Select(x => (IDictionary<string, object>) x);
        }

        public static Dictionary<string, string> GetDocumentField(List<string> documentIds, string desiredField)
        {
            var sql = $"SELECT DocNumber, {desiredField} as Value FROM {GetDatabaseName()}.[Document] WHERE DocNumber in @documentIds";
            return SqlExecutor.Query<KeyValuePair<string, string>>(sql, new { documentIds })                
                    .ToDictionary(x => x.Key, x => x.Valuel);
        }

        public static IEnumerable<ColumnDetails> GetColumnDetails(string caseId)
        {
            return SqlExecutor.Query<ColumnDetails>($"SELECT * FROM [eDiscoveryToolbox.Case.{caseId}].[dbo].[ColumnDetails]");
        }

        public static IEnumerable<ColumnDetails> GetColumnDetails()
        {
            var columns = SqlExecutor.Query<ColumnDetails>($"SELECT * FROM {GetDatabaseName()}.[ColumnDetails]");

            using (var sw = new StreamWriter(Path.Combine(Settings.LogDirectory, "edt_db_cols.csv")))
            {
                sw.WriteLine("id,displayname,columnname");

                foreach (var column in columns)
                {
                    sw.WriteLine($"{column.ColumnDetailsID},{column.DisplayName},{column.ColumnName}");
                }
            }

            return columns;
        }

        public static long GetDocumentCount()
        {
            var sql = $"SELECT Count(document.DocumentId) FROM {GetDatabaseName()}.[Batch] batch"
                        + $" INNER JOIN {GetDatabaseName()}.[Document] document ON batch.BatchID = document.BatchID"
                        + $" WHERE batch.BatchName = '{Settings.EdtImporterDatasetName}'";

            return SqlExecutor.QueryFirstOrDefault<long>(sql);
        }

        public static List<string> GetDocuentNumbersWithABody()
        {
            var sql = $"SELECT document.DocumentId FROM {GetDatabaseName()}.[Batch] batch"
                        + $" INNER JOIN {GetDatabaseName()}.[Document] document ON batch.BatchID = document.BatchID"
                        + $" WHERE batch.BatchName = '{Settings.EdtImporterDatasetName}'" 
                        + $" AND document.Body IS NOT NULL";

            return SqlExecutor.Query<string>(sql).ToList();
        }

        public static List<string> GetDocumentNumbers()
        {
            var sql = $"SELECT document.DocNumber FROM {GetDatabaseName()}.[Batch] batch"
                        + $" INNER JOIN {GetDatabaseName()}.[Document] document ON batch.BatchID = document.BatchID"
                        + $" WHERE batch.BatchName = '{Settings.EdtImporterDatasetName}'";

            return SqlExecutor.Query<string>(sql).ToList();
        }

        public static List<DocumentCorrespondant> GetDocumentCorrespondances(List<string> documentIds)
        {
            var sql = "SELECT  document.DocNumber, party.PartyName, correspondenceType.CorrespondenceTypeName as CorrespondanceType"
                + $" FROM {GetDatabaseName()}.[Document] document"
                + $" INNER JOIN {GetDatabaseName()}.[DocumentParty] docParty ON document.DocumentID = docParty.DocumentID"
                + $" INNER JOIN {GetDatabaseName()}.[Party] party ON docParty.PartyID = party.PartyID"
                + $" INNER JOIN {GetDatabaseName()}.[CorrespondenceType] correspondenceType ON docParty.CorrespondenceTypeID = correspondenceType.CorrespondenceTypeID"
                + $" WHERE document.DocNumber in @documentIds";

            return SqlExecutor.Query<DocumentCorrespondant>(sql, new { documentIds }).ToList();
        }

        public static IEnumerable<DerivedFileLocation> GetNativeFileLocations()
        {
            var sql = "SELECT CONCAT(CONVERT(varchar(10), document.DocumentID), '_NATIVE',FileExtOrMsgClass) as FileName, FolderID, DocumentID " +
                     $"FROM {GetDatabaseName()}.[Batch] batch " +
                     $"INNER JOIN {GetDatabaseName()}.[Document] document ON batch.BatchID = document.BatchID " +
                     $"WHERE batch.BatchName = '{Settings.EdtImporterDatasetName}'";

            return SqlExecutor.Query<DerivedFileLocation>(sql);
        }

        private static string GetConnectionStringByName()
        {
            return ConfigurationManager.AppSettings[EdtConnectionStringConfigKey];
        }

        private static string GetDatabaseName()
        {
            var caseId = Settings.EdtCaseId.PadLeft(3, '0');

            return $"[eDiscoveryToolbox.Case.{caseId}].[dbo]";

        }

    }
}
