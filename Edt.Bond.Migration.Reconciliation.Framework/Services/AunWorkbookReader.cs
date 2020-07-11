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
                        ParentID = line[8],
                        FullTagHierarchy = new List<string>() { line[8]}
			        });
		        } 
	        }

	        tags.Where(c => c.Level == 0).ToList().ForEach(c =>
	        {
				c.FullPath = $"Ems Folders:{c.Name.Trim()}";
                c.FullPathCleaned = c.FullPath.ReplaceTagChars();
                c.FullPathOutput = c.FullPath;

                var children = tags.Where(d => d.ParentID == c.Id).ToList();

		        if (children.Any())
		        {
			        SetFullPath(c, children, tags);

		        } 
	        }); 

			 return tags;
        }

        private static void SetFullPath(Tag parent, List<Tag> parents, List<Tag> tags)
        {
	        parents.ForEach(c =>
	        { 
		        c.FullPath = parent.FullPath + ":" + c.Name.Trim();
                c.FullPathCleaned = parent.FullPathCleaned.ReplaceTagChars();
                c.FullPathOutput = parent.FullPathOutput + @"\\" + c.Name.TrimEnd();                
                c.FullTagHierarchy.AddRange(parent.FullTagHierarchy);
                c.FullTagHierarchy.Add(c.ParentID);

                var children = tags.Where(d => d.ParentID == c.Id && c.Id != d.Id).ToList();

		        if (children.Any())
		        {
			        SetFullPath(c, children, tags);

		        } 
	        });
		}
    }


}
