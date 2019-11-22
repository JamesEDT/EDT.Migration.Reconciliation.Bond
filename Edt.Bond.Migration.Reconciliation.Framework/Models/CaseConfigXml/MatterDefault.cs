using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.CaseConfigXml
{
    [Serializable, XmlRoot(ElementName = "matterDefault")]
    public class MatterDefault
    {
        [XmlElement(ElementName = "dpModule")]
        public DpModule DpModule { get; set; }

        [XmlElement(ElementName = "rootfolder")]
        public RootFolder RootFolder { get; set; }

        [XmlArray(ElementName = "folders")]
        [XmlArrayItem(ElementName = "folder")]
        public Folder[] Folders { get; set; }

        [XmlArray("workstreams", IsNullable = true)]
        [XmlArrayItem("workstream")]
        public List<WorkStream> Workstreams { get; set; }

        [XmlArray("codingrules")]
        [XmlArrayItem("codingrule")]
        public CodingRule[] CodingRules { get; set; }

        [XmlElement(ElementName = "reports")]
        public Reports Reports { get; set; }

        [XmlElement(ElementName = "tags")]
        public TagsContainer TagsContainer { get; set; }

        [XmlElement(ElementName = "reviewfields")]
        public ReviewFields ReviewFields { get; set; }

        [XmlArray("redactionSets")]
        [XmlArrayItem("redactionSet")]
        public RedactionSet[] RedactionSets { get; set; }

        [XmlArray("settings")]
        [XmlArrayItem("setting")]
        public Setting[] Settings { get; set; }

        [XmlElement("viewTitleTemplate")]
        public ViewTitleTemplate ViewTitleTemplate { get; set; }

        [XmlArray("roelspermission")]
        [XmlArrayItem("role")]
        public Role[] RolesPermission { get; set; }
    }
}
