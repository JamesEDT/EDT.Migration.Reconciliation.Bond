using Edt.Bond.Migration.Reconciliation.Framework.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tag = Edt.Bond.Migration.Reconciliation.Framework.Models.Conversion.Tag;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class AunWorkbookReader
    {
        public static List<Tag> Read()
        {
	        var tags = new List<Tag>();

	        using (var streamReader = new StreamReader(Settings.MicroFocusAunWorkbookPath))
	        {
		        var headers = streamReader.ReadLine(); 

		        while (!streamReader.EndOfStream)
		        {
			        var line = streamReader.ReadLine()?.SplitCsv();

			        tags.Add(new Tag
			        {
				        Id = line[0],
				        Level = int.Parse(line[4]),
				        Name = line[1],
						  ParentID = line[8]
			        });
		        } 
	        }

	        tags.Where(c => c.ParentID == "NULL").ToList().ForEach(c =>
	        {
				  c.FullPath = c.Name;

				  var children = tags.Where(d => d.ParentID == c.Id).ToList();

		        if (children.Any())
		        {
			        SetFullPath(c.Name, children, tags);

		        } 
	        }); 
			 return tags;
        }

        private static void SetFullPath(string parentName, List<Tag> parents, List<Tag> tags)
        {
	        parents.ForEach(c =>
	        { 
		        c.FullPath = parentName + @"\" + c.Name; 

		        var children = tags.Where(d => d.ParentID == c.Id && c.Id != d.Id).ToList();

		        if (children.Any())
		        {
			        SetFullPath(c.FullPath, children, tags);

		        } 
	        });
		}
    }


}
