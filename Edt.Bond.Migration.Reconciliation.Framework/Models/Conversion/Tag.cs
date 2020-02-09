using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion
{
	public class Tag
	{
		public string ParentID { get; set; }
		public int Level { get; set; }
		public string Name { get; set; }
		public string Id { get; set; }
		public string FullPath { get; set; }
        public string FullPathOutput { get; set; }
        public string FullPathCleaned { get; set; }

        public List<string> FullTagHierarchy { get; set; }
	}
}
