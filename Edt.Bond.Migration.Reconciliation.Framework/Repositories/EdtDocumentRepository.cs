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

        public static IEnumerable<IDictionary<string, object>> GetDocuments(List<string> documentIds)
        {
            var sql = $"SELECT * FROM {GetDatabaseName()}.[Document] doc WHERE {GetDocumentIDQuery(documentIds)}";
            return SqlExecutor.Query(sql).Select(x => (IDictionary<string, object>)x);
        }

        public static Dictionary<string, string> GetDocumentField(List<string> documentIds, string desiredField)
        {
            var sql = $@"SELECT DocNumber, {desiredField} as Value 
                            FROM {GetDatabaseName()}.[Document] doc
                            LEFT OUTER JOIN {GetDatabaseName()}.[DocumentExtra] docExtra ON doc.DocumentID = docExtra.DocumentID
                        WHERE {GetDocumentIDQuery(documentIds)}";

            return SqlExecutor.Query(sql)
                    .ToDictionary(x => (string)x.DocNumber, x => (string)x.Value?.ToString() ?? string.Empty);
        }

        public static string GetDocumentIDQuery(List<string> documentIds)
        {
            if (Settings.IdxSampleSize == 0)
            {
               return $@" exists (select d.documentID from  {GetDatabaseName()}.[document] d inner join  
                        {GetDatabaseName()}.[batch] b on b.batchId = d.batchId and b.batchName = '{Settings.EdtImporterDatasetName}'
                            where doc.Documentid = d.documentID)";
            }
            else
            {
                return "DocNumber in (" + string.Join(",", documentIds.Select(x => $"'{x}'")) + ")";
            }
        }

        public static Dictionary<string, string> GetDocumentDateField(List<string> documentIds, string desiredField)
        {
            var sql = $@"SELECT DocNumber, {desiredField} as Value 
                        FROM {GetDatabaseName()}.[Document] doc
                        LEFT OUTER JOIN {GetDatabaseName()}.[DocumentExtra] docExtra ON doc.DocumentID = docExtra.DocumentID
                        WHERE {GetDocumentIDQuery(documentIds)}";

            return SqlExecutor.Query(sql)
                .ToDictionary(x => (string)x.DocNumber, x => (string)GetDate(x));
        }

        private static string GetDate(dynamic x)
        {
            var value = ((IDictionary<string, object>)x).Values.Last();
            return value != null ? ((DateTime)value).ToString("dd/MM/yyyy HH:mm:ss") : "";
        }

        public static IEnumerable<ColumnDetails> GetColumnDetails()
        {
            var columns = SqlExecutor.Query<ColumnDetails>($"SELECT * FROM {GetDatabaseName()}.[ColumnDetails]");

            if (!File.Exists(Path.Combine(Settings.LogDirectory, "edt_db_cols.csv")))
            {
                using (var sw = new StreamWriter(Path.Combine(Settings.LogDirectory, "edt_db_cols.csv")))
                {
                    sw.WriteLine("id,displayname,columnname");

                    foreach (var column in columns)
                    {
                        sw.WriteLine($"{column.ColumnDetailsID},{column.DisplayName},{column.ColumnName}");
                    }
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

        public static List<string> GetDocumentNumbersWithABody()
        {
            var sql = $@"SELECT document.DocNumber FROM {GetDatabaseName()}.[Batch] batch
                         INNER JOIN {GetDatabaseName()}.[Document] document ON batch.BatchID = document.BatchID
                         WHERE batch.BatchName = '{Settings.EdtImporterDatasetName}'
                         AND document.Body IS NOT NULL
                         AND LEN(document.Body) > 0";

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
            var sql = "SELECT  doc.DocNumber as DocumentNumber, party.PartyName, correspondenceType.CorrespondenceTypeName as CorrespondanceType"
                + $" FROM {GetDatabaseName()}.[Document] doc"
                + $" INNER JOIN {GetDatabaseName()}.[DocumentParty] docParty ON doc.DocumentID = docParty.DocumentID"
                + $" INNER JOIN {GetDatabaseName()}.[Party] party ON docParty.PartyID = party.PartyID"
                + $" INNER JOIN {GetDatabaseName()}.[CorrespondenceType] correspondenceType ON docParty.CorrespondenceTypeID = correspondenceType.CorrespondenceTypeID"
                + $" WHERE {GetDocumentIDQuery(documentIds)}";

            return SqlExecutor.Query<DocumentCorrespondant>(sql).ToList();
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
            var sql = $@"SELECT doc.DocNumber as DocumentNumber, tag.FullName as Tag FROM {GetDatabaseName()}.[Document] doc
                          INNER JOIN {GetDatabaseName()}.[DocumentTag] documentTag ON doc.DocumentID = documentTag.DocumentID
                          INNER JOIN {GetDatabaseName()}.[Tag] tag ON documentTag.TagID = tag.TagID
                          WHERE {GetDocumentIDQuery(documentIds)}";

            var rawTags = SqlExecutor.Query(sql);

            var tags = from tag in rawTags
                       group (string)tag.Tag by tag.DocumentNumber into docTags
                       select new { DocumentId = (string)docTags.Key, Value = docTags };

            return tags.ToDictionary(x => x.DocumentId, x => x.Value.ToList());
        }

        public static Dictionary<string, string> GetDocumentLocations(List<string> documentIds)
        {
            var sql = $@"SELECT doc.DocNumber as DocumentNumber, l.Path as Location FROM {GetDatabaseName()}.[Document] doc
	                    INNER JOIN {GetDatabaseName()}.[Location] l ON l.LocationID = doc.LocationID
	                    WHERE {GetDocumentIDQuery(documentIds)}";

            var rawLocations = SqlExecutor.Query(sql);

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
								Document.DocNumber LIKE  '%_R'";

            return SqlExecutor.QueryFirstOrDefault<int>(sql);
        }


        public static IEnumerable<dynamic> GetMultiValueFieldValues(List<string> documentIds, string fieldName)
        {
            var sql = $@"SELECT
	                        doc.DocNumber,
	                        mvField.Name as FieldValue,
	                        mvField.DisplayOrder As MvFieldDisplayOrder,
	                        Field.Name
                          FROM {GetDatabaseName()}.[DocumentMvField] documentField
                          INNER JOIN {GetDatabaseName()}.[Document] doc on documentField.DocumentID = doc.DocumentID
                          INNER JOIN {GetDatabaseName()}.[MvField] mvField on documentField.MvFieldID = mvField.MvFieldID
                          INNER JOIN (
	                        SELECT 
			                        MvField.MvFieldID as Id,
			                        mvField.Name as Name
	                        FROM {GetDatabaseName()}.[MvField] mvField
	                        WHERE mvField.ParentID = 0
                          ) as Field on mvField.ParentID = Field.Id
                            And Field.Name = @fieldName
                        WHERE {GetDocumentIDQuery(documentIds)}";

            var multiValueListResults = SqlExecutor.Query(sql, new { fieldName });

            return multiValueListResults;
        }

        private static string GetConnectionStringByName()
        {
            return ConfigurationManager.AppSettings[EdtConnectionStringConfigKey];
        }

        public static string GetDatabaseName()
        {
            return $"[{ConfigurationManager.AppSettings["EdtCaseDatabaseName"]}].[dbo]";
        }

        public static IEnumerable<dynamic> GetAllDocumentIds()
        {
            var sql = $@"SELECT document.DocumentId as DocumentId, document.DocNumber as DocumentNumber
                        FROM {GetDatabaseName()}.[Batch] batch
                        INNER JOIN {GetDatabaseName()}.[Document] document ON batch.BatchID = document.BatchID
                        WHERE batch.BatchName = '{Settings.EdtImporterDatasetName}'";

            return SqlExecutor.Query<dynamic>(sql);
        }
    }
}
