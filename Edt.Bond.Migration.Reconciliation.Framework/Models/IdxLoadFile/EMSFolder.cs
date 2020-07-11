using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.IdxLoadFile
{
    public class EmsFolder
    {
        public string Source { get; set; }
        public string Group { get; set; }
        public string Custodian { get; set; }

        public List<string> VIRTUAL_PATH_SEGMENTs = new List<string>();

        private string _edtLocation;

        public string ConvertedEdtLocation
        {
            get
            {
                if (_edtLocation == null)
                {
                    _edtLocation = $@"{Group ?? string.Empty}\{Custodian ?? string.Empty}\{Source ?? string.Empty}";


                    VIRTUAL_PATH_SEGMENTs.ForEach(c =>
                        {
                            if (!string.IsNullOrWhiteSpace(c) && !c.Contains(".msg:"))
                                _edtLocation += @"\" + c.Replace(":", "-");
                        }
                    );
                }
                return _edtLocation;
            }
        }

        public string VirtualPathSegements
        {
            get
            {
                return VIRTUAL_PATH_SEGMENTs != null ?
                      string.Join("\\", VIRTUAL_PATH_SEGMENTs)
                     : string.Empty;
            }
        }
    }
}
