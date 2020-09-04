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
            var patternWithExtension = $"{documentId.Replace("/", string.Empty).ToLowerInvariant()}.";
            var matches =_nativeFiles.Where(x => x.Key.Equals(documentId.Replace("/", string.Empty).ToLowerInvariant()) || x.Key.StartsWith(patternWithExtension));

            return matches?.SingleOrDefault().Value?.Extension;
        }

        public IEnumerable<FileInfo> GetFiles(string documentId)
        {
            var pattern = $"{documentId.ToLowerInvariant()}.";
            var matches = _nativeFiles.Where(x => x.Key.StartsWith(pattern));

            return matches.Select(x => x.Value);
        }


        private IDictionary<string, FileInfo> GetAvailableFileList()
        {
            var settings = ConfigurationManager.AppSettings;

            var sourceLocation = settings["NativePath"];

            if (string.IsNullOrEmpty(sourceLocation))
                throw new Exception($"Settings file missing NativePath key");

            var allFiles = Directory.GetFiles(sourceLocation, "*.*", SearchOption.AllDirectories);

            var allFileInfos = allFiles
                .Select(x => new FileInfo(x))
                .Where(x => !x.Name.Equals("thumbs.db", StringComparison.InvariantCultureIgnoreCase))
                .Where(x => x.DirectoryName != null && x.DirectoryName.Contains($"\\NATIVE"))
                .ToList();

            return allFileInfos.ToDictionary(y => y.Name.ToLower());
        }
    }
}
