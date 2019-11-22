using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    [XmlType("setting")]
    public class Setting
    {
        [XmlAttribute("key")]
        public string _key { private get; set; }

        [XmlAttribute("name")]
        public string _name { private get; set; }

        public string Name
        {
            get
            {
                return _key ?? _name;
            }
        }

        [XmlAttribute("value")]
        public string Value { get; set; }
    }
}