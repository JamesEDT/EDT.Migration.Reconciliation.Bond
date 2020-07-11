using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Framework.Services
{
    public class NativeFileFinder
    {
        private IDictionary<string, FileInfo> _nativeFiles;

        public NativeFileFinder()
        {
            _nativeFiles = GetAvailableFileList();
        }

        public string GetExtension(string documentId)
        {
            var pattern = $"{documentId.ToLowerInvariant()}.";
            var matches =_nativeFiles.Where(x => x.Key.StartsWith(pattern));

            return matches?.SingleOrDefault().Value?.Extension;
        }


        private IDictionary<string, FileInfo> GetAvailableFileList()
        {
            var settings = ConfigurationManager.AppSettings;

            var sourceLocation = settings["NativePath"];

            if (string.IsNullOrEmpty(sourceLocation))
                throw new Exception($"Settings file missing NativePath key");

            var allFiles = Directory.GetFiles(sourceLocation, "*.*", SearchOption.AllDirectories);

            var allFileInfos = allFiles.Select(x => new FileInfo(x)).Where(x => !x.Name.Equals("thumbs.db", StringComparison.InvariantCultureIgnoreCase)).ToList();

            return allFileInfos.ToDictionary(y => y.Name.ToLower());
        }
    }
}
