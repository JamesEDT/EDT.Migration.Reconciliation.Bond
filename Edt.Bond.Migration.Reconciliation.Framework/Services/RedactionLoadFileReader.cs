using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
	public class RedactionLoadFileReader
	{

		public RedactionLoadFileReader()
		{
			using (var reader = new StreamReader(_path))
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
				string directory = Path.GetDirectoryName(_path);

				var docs = records as dynamic[] ?? records.ToArray();
			}
		}
	}
}
