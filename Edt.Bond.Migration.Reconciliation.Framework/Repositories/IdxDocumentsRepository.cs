using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using LiteDB;

namespace Edt.Bond.Migration.Reconciliation.Framework.Repositories
{
    public class IdxDocumentsRepository
    {
        public string DbName
        {
            get
            {
                if(_dbName == null) Initialise(false);
                return _dbName;
            }
        }

        public string _dbName;

        public void AddDocument(Document document)
        {
            using (var db = new LiteDatabase(DbName))
            {
                var documentsTable = db.GetCollection<Document>("Documents");

                documentsTable.Insert(document);                
            }
        }

        public void Initialise(bool deleteExistingDb = true)
        {
            var caseId = ConfigurationManager.AppSettings["EdtCaseId"];
            var idxName = ConfigurationManager.AppSettings["IdxName"]?.Replace(".", string.Empty);

            _dbName = $".\\IdxRepo_{caseId}_{idxName}.db";

            if(deleteExistingDb && File.Exists(DbName))
                File.Delete(DbName);
        }

        public static bool Exists()
        {
            var caseId = ConfigurationManager.AppSettings["EdtCaseId"];
            var idxName = ConfigurationManager.AppSettings["IdxName"]?.Replace(".", string.Empty);

            var dbName = $".\\IdxRepo_{caseId}_{idxName}.db";

            return File.Exists(dbName);
               
        }

        public void AddDocuments(IEnumerable<Document> documents)
        {
            using (var db = new LiteDatabase(DbName))
            {
                var documentsTable = db.GetCollection<Document>("Documents");

                documentsTable.InsertBulk(documents);
            }
        }

        public void CreateDocumentIdIndex()
        {
            using (var db = new LiteDatabase(DbName))
            {
                var documents = db.GetCollection<Document>("Documents");

                // Index document using a document property
                documents.EnsureIndex(x => x.DocumentId);
            }
        }

        public Document GetDocument(string uuid)
        {
            using (var db = new LiteDatabase(DbName))
            {
                var documents = db.GetCollection<Document>("Documents");

                // Use Linq to query documents
                return documents.FindOne(x => x.DocumentId.Equals(uuid));
            }
        }

        public List<string> GetDocumentIds()
        {
            using (var db = new LiteDatabase(DbName))
            {
                var documents = db.GetCollection<Document>("Documents");

                // Use Linq to query documents
                return documents.FindAll().Select(x => x.DocumentId).ToList();
            }
        }

        public IEnumerable<Document> GetDocuments(System.Linq.Expressions.Expression<System.Func<Document, bool>> expression)
        {
            using (var db = new LiteDatabase(DbName))
            {
                var documents = db.GetCollection<Document>("Documents");

                // Use Linq to query documents
                return documents.Find(expression);
            }
        }

        public long GetNumberOfDocuments()
        {
            using (var db = new LiteDatabase(DbName))
            {
                var documents = db.GetCollection<Document>("Documents");

                return documents.LongCount();
            }
        }

        public int GetNumberOfLppDocs()
        {
	        using (var db = new LiteDatabase(DbName))
	        {
		        var documents = db.GetCollection<Document>("Documents");

		        return documents.FindAll().Count(x => Enumerable.Any(x.AllFields, c => c.Key == "INTROSPECT_DELETED" && !string.IsNullOrWhiteSpace(c.Value))); 
	        }

        }

        public IEnumerable<Document> GetSample()
        {
            using (var db = new LiteDatabase(DbName))
            {
                var documents = db.GetCollection<Document>("Documents");

                var skip = Settings.IdxSampleSize != 0 ? (int) (GetNumberOfDocuments() / Settings.IdxSampleSize) : 0;
                var requiredRecords = Settings.IdxSampleSize != 0 ? Settings.IdxSampleSize : (int) GetNumberOfDocuments();

                return documents.Find(x => x.DocumentId != null, skip, requiredRecords);
            }
        }

        public long GetColumnSizeOfDocuments()
        {
            using (var db = new LiteDatabase(DbName))
            {
                var documents = db.GetCollection<Document>("Documents");

                return (long) documents.FindAll().Select(x => x.AllFields.Count).Average();
            }
        }
    }
}
