
using System.Collections.Generic;
//
namespace Contensive.Processor.Models {
    /// <summary>
    /// The structure of an included addon list, stored in page and template records
    /// get and put remote methods in Processor addons
    /// </summary>
    [System.Serializable]
    public class AddonPanelListItemModel {
        /// <summary>
        /// The guid of the design block addon
        /// </summary>
        public string designBlockTypeGuid;
        /// <summary>
        /// The name of the design block addon
        /// </summary>
        public string name;
        /// <summary>
        /// imageHref - The full URI of an image that shows the user what it does. Not an icon, but a representation
        /// </summary>
        public string image;
        /// <summary>
        /// columnList - if the addon creates columns, the list of features for each column
        /// </summary>
        public List<AddonPanelListItemColumnModel> columns;
        /// <summary>
        /// placeholder for future feature
        /// </summary>
        public string renderedHtml;
        /// <summary>
        /// The addon category from the addon record
        /// </summary>
        public string category;
    }
    //
    [System.Serializable]
    public class AddonPanelListItemColumnModel {
        /// <summary>
        /// 
        /// </summary>
        public string className;
        public int col;
    }
}