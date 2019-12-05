using Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion;
using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase;
using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase.Dto;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
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
            return SqlExecutor.Query(sql, new { documentIds }).Select(x => (IDictionary<string, object>)x);
        }

        public static Dictionary<string, string> GetDocumentField(List<string> documentIds, string desiredField)
        {
            var sql = $@"SELECT DocNumber, {desiredField} as Value 
                            FROM {GetDatabaseName()}.[Document] doc
                            LEFT OUTER JOIN {GetDatabaseName()}.[DocumentExtra] docExtra ON doc.DocumentID = docExtra.DocumentID
                        WHERE DocNumber in @documentIds";

            return SqlExecutor.Query(sql, new { documentIds })
                    .ToDictionary(x => (string)x.DocNumber, x => (string)x.Value?.ToString() ?? string.Empty);
        }

        public static Dictionary<string, string> GetDocumentDateField(List<string> documentIds, string desiredField)
        {
	        var sql = $@"SELECT DocNumber, {desiredField} as Value 
                        FROM {GetDatabaseName()}.[Document] doc
                        LEFT OUTER JOIN {GetDatabaseName()}.[DocumentExtra] docExtra ON doc.DocumentID = docExtra.DocumentID
                        WHERE DocNumber in @documentIds";

	        return SqlExecutor.Query(sql, new { documentIds })
		        .ToDictionary(x => (string)x.DocNumber, x => (string)x.Value?.ToString() == "" ? "" : ((DateTime)((IDictionary<string, object>)x).Values.Last()).ToString("dd/MM/yyyy HH:mm:ss"));
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
            var sql = $@"SELECT Count(document.DocumentId) FROM {GetDatabaseName()}.[Batch] batch
                         INNER JOIN {GetDatabaseName()}.[Document] document ON batch.BatchID = document.BatchID
                         WHERE batch.BatchName = '{Settings.EdtImporterDatasetName}'";

            return SqlExecutor.QueryFirstOrDefault<long>(sql);
        }

        public static IEnumerable<dynamic> GetDocumentCountPerBatch()
        {
            var sql = $@"SELECT batch.BatchName as BatchName, Count(document.DocumentId) as DocumentCount FROM {GetDatabaseName()}.[Batch] batch
                         INNER JOIN {GetDatabaseName()}.[Document] document ON batch.BatchID = document.BatchID
                         GROUP BY batch.BatchName";                        

            return SqlExecutor.Query<dynamic>(sql);
        }

        public static List<string> GetDocuentNumbersWithABody()
        {
            var sql = $@"SELECT document.DocumentId FROM {GetDatabaseName()}.[Batch] batch
                         INNER JOIN {GetDatabaseName()}.[Document] document ON batch.BatchID = document.BatchID
                         WHERE batch.BatchName = '{Settings.EdtImporterDatasetName}'
                         AND document.Body IS NOT NULL";

            return SqlExecutor.Query<string>(sql).ToList();
        }

        public static List<string> GetDocumentNumbers()
        {
            var sql = $@"SELECT document.DocNumber FROM {GetDatabaseName()}.[Batch] batch
                         INNER JOIN {GetDatabaseName()}.[Document] document ON batch.BatchID = document.BatchID
                         WHERE batch.BatchName = '{Settings.EdtImporterDatasetName}'";

            return SqlExecutor.Query<string>(sql).ToList();
        }

        public static List<DocumentCorrespondant> GetDocumentCorrespondances(List<string> documentIds)
        {
            var sql = "SELECT  document.DocNumber as DocumentNumber, party.PartyName, correspondenceType.CorrespondenceTypeName as CorrespondanceType"
                + $" FROM {GetDatabaseName()}.[Document] document"
                + $" INNER JOIN {GetDatabaseName()}.[DocumentParty] docParty ON document.DocumentID = docParty.DocumentID"
                + $" INNER JOIN {GetDatabaseName()}.[Party] party ON docParty.PartyID = party.PartyID"
                + $" INNER JOIN {GetDatabaseName()}.[CorrespondenceType] correspondenceType ON docParty.CorrespondenceTypeID = correspondenceType.CorrespondenceTypeID"
                + $" WHERE document.DocNumber in @documentIds";

            return SqlExecutor.Query<DocumentCorrespondant>(sql, new { documentIds }).ToList();
        }

        public static IEnumerable<DerivedFileLocation> GetNativeFileLocations()
        {
            var sql = $@"SELECT CONCAT(CONVERT(varchar(10), document.DocumentID), '_NATIVE',FileExtOrMsgClass) as FileName, DocumentID as DocumentId
                         FROM {GetDatabaseName()}.[Batch] batch
                         INNER JOIN {GetDatabaseName()}.[Document] document ON batch.BatchID = document.BatchID
                         WHERE batch.BatchName = '{Settings.EdtImporterDatasetName}'";

            return SqlExecutor.Query<DerivedFileLocation>(sql);
        }

        public static Dictionary<string, List<string>> GetDocumentTags(List<string> documentIds)
        {
            var sql = $@"SELECT document.DocNumber as DocumentNumber, tag.TagName as Tag FROM {GetDatabaseName()}.[Document] document
                          INNER JOIN {GetDatabaseName()}.[DocumentTag] documentTag ON document.DocumentID = documentTag.DocumentID
                          INNER JOIN {GetDatabaseName()}.[Tag] tag ON documentTag.TagID = tag.TagID
                          WHERE document.DocNumber in @documentIds";

            var rawTags = SqlExecutor.Query(sql, new { documentIds });

            var tags = from tag in rawTags
                       group (string) tag.Tag by tag.DocumentNumber into docTags
                       select new { DocumentId = (string) docTags.Key, Value = docTags };

             return tags.ToDictionary(x => x.DocumentId, x => x.Value.ToList());
        }

        public static Dictionary<string, string> GetDocumentLocations(List<string> documentIds)
        {
	        var sql = $@"SELECT document.DocNumber as DocumentNumber, l.Path as Location FROM {GetDatabaseName()}.[Document] document
	                    INNER JOIN {GetDatabaseName()}.[Location] l ON l.LocationID = document.LocationID
	                    WHERE document.DocNumber in @documentIds";

	        var rawLocations = SqlExecutor.Query(sql, new { documentIds });

		    return rawLocations.ToDictionary(x => (string)x.DocumentNumber, x => (string)x.Location); 
        }

        public static int GetDocumentQuarantineDocumentCount()
        {
	        var sql =
		        $@"SELECT Count(document.DocumentId) FROM {GetDatabaseName()}.[Batch] batch 
						INNER JOIN {GetDatabaseName()}.[Document] document ON batch.BatchID = document.BatchID
						INNER JOIN {GetDatabaseName()}.[Folder] folder ON folder.FolderId = document.FolderID
						WHERE batch.BatchName = '{Settings.EdtImporterDatasetName}' AND
								folder.folderType = 1 --Quarantine Folder";

	        return SqlExecutor.QueryFirstOrDefault<int>(sql);
        }

        public static int GetDocumentRedactedDocumentCount()
        {
	        var sql =
		        $@"SELECT Count(document.DocumentId) FROM {GetDatabaseName()}.[Batch] batch 
						INNER JOIN {GetDatabaseName()}.[Document] document ON batch.BatchID = document.BatchID 
						WHERE batch.BatchName = '{Settings.EdtImporterDatasetName}' AND
								Document.DocNumber LIKE  '_R%'";

	        return SqlExecutor.QueryFirstOrDefault<int>(sql);
        } 


        public static IEnumerable<dynamic> GetMultiValueFieldValues(List<string> documentIds, string fieldName)
        {
            var sql = $@"SELECT
	                        document.DocNumber,
	                        mvField.Name as FieldValue,
	                        mvField.DisplayOrder As MvFieldDisplayOrder,
	                        Field.Name
                          FROM [eDiscoveryToolbox.Case.011].[dbo].[DocumentMvField] documentField
                          INNER JOIN [eDiscoveryToolbox.Case.011].[dbo].[Document] document on documentField.DocumentID = document.DocumentID
                          INNER JOIN [eDiscoveryToolbox.Case.011].[dbo].[MvField] mvField on documentField.DocumentMvFieldID = mvField.MvFieldID
                          INNER JOIN (
	                        SELECT 
			                        MvField.MvFieldID as Id,
			                        mvField.Name as Name
	                        FROM [eDiscoveryToolbox.Case.011].[dbo].[MvField] mvField
	                        WHERE mvField.ParentID = 0
                          ) as Field on mvField.ParentID = Field.Id
                        WHERE DocNumber in @documentIds
                        AND Field.Name = @fieldName";

            var multiValueListResults = SqlExecutor.Query(sql, new { documentIds, fieldName });

            return multiValueListResults;
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
