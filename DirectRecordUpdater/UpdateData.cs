using Edt.Bond.Migration.Reconciliation.Framework.Repositories.EdtRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectRecordUpdater
{
    class UpdateData
    {
        public static void Update(string table, string id, string newValue)
        {
            switch(table.ToLower())
            {
                case "party":
                    PartyRepository.UpdatePartyName(id, newValue);
                    break;
                    
                default:
                    throw new ArgumentException($"Unknown table {table}");
            }
        }


    }
}
