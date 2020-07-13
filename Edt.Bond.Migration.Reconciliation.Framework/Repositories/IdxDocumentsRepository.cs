using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile;
using LiteDB;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            
            _dbName = GetDbName();

            if(deleteExistingDb && File.Exists(DbName))
                File.Move(DbName, $"{DbName}_{DateTime.Now.Ticks}");
        }

        private static string GetDbName()
        {
            var caseId = ConfigurationManager.AppSettings["EdtCaseId"];
            var idxPath = ConfigurationManager.AppSettings["IdxFilePath"].Split(new char[] { '|' });
            if (idxPath.Length == 1)
            {
                var idxName = string.IsNullOrEmpty(idxPath.First()) ? string.Empty : new FileInfo(idxPath.First()).Name.Replace(".", string.Empty);

                return $"{Settings.LogDirectory}\\IdxRepo_{caseId}_{idxName}.db";
            }

            return $"{Settings.LogDirectory}\\IdxRepo_{caseId}_Multi.db";
        }

        public static bool Exists()
        {

            return File.Exists(GetDbName());
               
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
                try
                {
                    // Index document using a document property
                    documents.EnsureIndex(x => x.DocumentId);
                }
                catch(Exception)
                {

                }
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

        public IEnumerable<Document> GetDocuments(IEnumerable<string> docIds)
        {
            using (var db = new LiteDatabase(DbName))
            {
                var documents = db.GetCollection<Document>("Documents");

                // Use Linq to query documents
                return documents.Find(Query.Where("DocumentId", x => docIds.ToList().Contains(x)));
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

        public IEnumerable<Document> GetSample(int skip = 0, int take = 100)
        {
            using (var db = new LiteDatabase(DbName))
            {
                var documents = db.GetCollection<Document>("Documents");

                return documents.Find(x => x.DocumentId != null, skip, take);
                
            }
        }

        public IEnumerable<Document> GetSample()
        {
            using (var db = new LiteDatabase(DbName))
            {
                var documents = db.GetCollection<Document>("Documents");

                if (Settings.IdxSampleSize != 0)
                {
                    var skip = (int)(GetNumberOfDocuments() / Settings.IdxSampleSize);

                    return documents.Find(x => x.DocumentId != null, skip, Settings.IdxSampleSize);
                }
                else
                {
                    return documents.FindAll();
                }
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
