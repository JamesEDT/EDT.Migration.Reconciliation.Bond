using System.IO;
using System.Linq;
using CsvHelper;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
	public class RedactionLoadFileReader
	{
        private dynamic[] _records;

		public RedactionLoadFileReader(string path)
		{
			using (var reader = new StreamReader(path))
			using (var csv = new CsvReader(reader))
			{
				csv.Configuration.Delimiter = ",";
				csv.Configuration.Quote = '"';

				csv.Configuration.BadDataFound = context =>
				{
					//ignore
				};

				csv.Configuration.HeaderValidated = null;
				csv.Configuration.MissingFieldFound = null;

				var records = csv.GetRecords<dynamic>();

				_records = records as dynamic[] ?? records.ToArray();
			}
		}

        public int GetRecordCount()
        {
            return _records.Length;
        }
	}
}
