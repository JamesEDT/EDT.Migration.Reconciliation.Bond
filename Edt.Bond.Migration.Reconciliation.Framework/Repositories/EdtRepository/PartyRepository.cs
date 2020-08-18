using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edt.Bond.Migration.Reconciliation.Framework.Repositories.EdtRepository
{
    public class PartyRepository: EdtDocumentRepository
    {
        public static void UpdatePartyName(string id, string value )
        {
            var sql = $"Update {GetDatabaseName()}.Party SET PartyName = @value WHERE PartyID = @id";

            SqlExecutor.ExecuteScalar(sql, new { id, value });
        }
    }
}
