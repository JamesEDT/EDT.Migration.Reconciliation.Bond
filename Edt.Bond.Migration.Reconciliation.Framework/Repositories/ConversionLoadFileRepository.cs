using System.Configuration;
using System.IO;

namespace Edt.Bond.Migration.Reconciliation.Framework.Repositories
{
    public class ConversionLoadFileRepository : IdxDocumentsRepository
    {
        public new void Initialise(bool deleteExistingDb = true)
        {
            var caseId = ConfigurationManager.AppSettings["EdtCaseId"];
            var idxName = ConfigurationManager.AppSettings["IdxName"]?.Replace(".", string.Empty);

            _dbName = $".\\LoadFileRepo_{caseId}_{idxName}.db";

            if(deleteExistingDb && File.Exists(DbName))
                File.Delete(DbName);
        }
    }
}
