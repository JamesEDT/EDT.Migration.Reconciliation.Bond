using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase;

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

        public static IEnumerable<ColumnDetails> GetColumnDetails(string caseId)
        {
            return SqlExecutor.Query<ColumnDetails>($"SELECT * FROM [eDiscoveryToolbox.Case.{caseId}].[dbo].[ColumnDetails]");
        }

        private static string GetConnectionStringByName()
        {
            return ConfigurationManager.AppSettings[EdtConnectionStringConfigKey];
        }
    }
}
