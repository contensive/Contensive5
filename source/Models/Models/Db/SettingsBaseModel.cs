
using Contensive.BaseClasses;

namespace Contensive.Models.Db {
    //
    /// <summary>
    /// Base model for design block settings.
    /// Inherit this model in Db models used as design block settings.
    /// This model includes the required fields for the Design Block pattern.
    /// The settings record is the Db record that represents the instance of the design block in one position.
    /// </summary>
    public class SettingsBaseModel : DbBaseModel {
        //
        // ====================================================================================================
        // -- Design block instance properties
        //
        /// <summary>
        /// Background image for the entire widget
        /// </summary>
        public string backgroundImageFilename { get; set; }
        //
        /// <summary>
        /// Select a theme (CSS class selector) to be used for the widget
        /// </summary>
        public int themeStyleId { get; set; }
        //
        /// <summary>
        /// Add padding on the top side
        /// </summary>
        public bool padTop { get; set; }
        //
        /// <summary>
        /// Add padding on the bottom side
        /// </summary>
        public bool padBottom { get; set; }
        //
        /// <summary>
        /// Add padding on the right side
        /// </summary>
        public bool padRight { get; set; }
        //
        /// <summary>
        /// Add padding on the left side
        /// </summary>
        public bool padLeft { get; set; }
        //
        /// <summary>
        /// Hardcoded height with units if needed (e.g., "500px", "20em")
        /// </summary>
        public string styleheight { get; set; }
        //
        /// <summary>
        /// If false, a Bootstrap container is added to the widget; if true, the widget is treated as full-bleed (full-width)
        /// </summary>
        public bool asFullBleed { get; set; }
        //
        /// <summary>
        /// Optional anchor target ID for linking to this widget (creates HTML id attribute)
        /// </summary>
        public string anchorTag { get; set; }
        //
        /// <summary>
        /// Animation style (1=No Animation, 2=Fade In Up, 3=Fade In Down, 4=Fade In Left, 5=Fade In Right, 6=Bounce In, 7=Zoom In)
        /// </summary>
        public int animateStyleId { get; set; }
        //
        /// <summary>
        /// Animation speed (maps to AnimateSpeedEnum: 1=Slow, 2=Normal, 3=Fast)
        /// </summary>
        public int animateSpeedId { get; set; }
        //
        // ====================================================================================================
        /// <summary>
        /// Virtual method for creating or adding settings. Override in derived classes.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="settingsGuid"></param>
        /// <param name="recordNameSuffix"></param>
        /// <returns></returns>
        public virtual SettingsBaseModel createOrAddSettings(CPBaseClass cp, string settingsGuid, string recordNameSuffix) {
            return new SettingsBaseModel();
        }
    }
}
