using System;
using System.IO;
using System.Linq;

namespace Edt.Bond.Migration.Reconciliation.Framework.Extensions
{
    public class NativeExtension
    {
        private static string _caseStoreLocation;
     

        public static bool IsNativePresent(int edtId)
        {
           var folder = (long)(Math.Floor(edtId / 5000.0) * 5000.0);

            if(_caseStoreLocation == null)
             _caseStoreLocation = Path.Combine(Settings.EdtCfsDirectory, $"Site01_Case{Settings.EdtCaseId.PadLeft(4, '0')}\\Docs");

            var folderPath = Path.Combine(_caseStoreLocation, $"{folder}\\{edtId}");

            try
            {
                return Directory.GetFiles(folderPath, $"{edtId}_NATIVE*").Any();
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
