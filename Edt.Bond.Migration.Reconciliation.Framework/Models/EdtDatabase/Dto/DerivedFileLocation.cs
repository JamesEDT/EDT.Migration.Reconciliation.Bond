﻿using System;
using System.IO;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase.Dto
{
    public class DerivedFileLocation
    {
        public string Filename { get; set; }

        public long FolderId => (long) (Math.Floor(DocumentId / 5000.0) * 5000.0);
        public long DocumentId { get; set; }

        public string FullDocumentPath => BuildFullPath();


        private string BuildFullPath()
        {
            var caseStoreLocation = Path.Combine(Settings.EdtCfsDirectory, $"Site01_Case{Settings.EdtCaseId.PadLeft(4, '0')}\\Docs");

            var filePath = Path.Combine(caseStoreLocation, $"{FolderId}\\{DocumentId}\\{Filename}");

            return filePath;            
        }       
    }
}
