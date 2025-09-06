
using System;
using Contensive.BaseClasses;

namespace Contensive.BaseClasses.LayoutBuilder {
    /// <summary>
    /// Create a Tool Form. Tool Forms how rows of input elements
    /// </summary>
    [Obsolete("Deprecated. Use cp.AdminUi.CreateLayoutBuilder", false)] public abstract class LayoutBuilderToolFormBaseClass : LayoutBuilderBaseClass {
        //
        public LayoutBuilderToolFormBaseClass(CPBaseClass cp) : base(cp) { }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add padding around the body
        /// </summary>
        public abstract bool IncludeBodyPadding { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add background color to the body
        /// </summary>
        public abstract bool IncludeBodyColor { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Set true if this tool is requested directly and not embedded in another AdminUI form
        /// </summary>
        public abstract bool IsOuterContainer { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The title of the form
        /// </summary>
        public abstract string Title { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// When an action is requested, use this position to inform success
        /// </summary>
        public abstract string SuccessMessage { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// When an action is requested, use this position to inform info
        /// </summary>
        public abstract string InfoMessage { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// When an action is requested, use this position to inform warn
        /// </summary>
        public abstract string WarningMessage { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// When an action is requested, use this position to inform error
        /// </summary>
        public abstract string FailMessage { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Description appears right below title
        /// </summary>
        public abstract string Description { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add hidden form input
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        public abstract void AddFormHidden(string Name, string Value);
        /// <summary>
        /// Add hidden form input
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public abstract void AddFormHidden(string name, int value);
        /// <summary>
        /// Add hidden form input
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public abstract void AddFormHidden(string name, double value);
        /// <summary>
        /// Add hidden form input
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public abstract void AddFormHidden(string name, DateTime value);
        /// <summary>
        /// Add hidden form input
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public abstract void AddFormHidden(string name, bool value);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add form button
        /// </summary>
        /// <param name="buttonValue"></param>
        public abstract void AddFormButton(string buttonValue);
        /// <summary>
        /// Add form button
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        public abstract void AddFormButton(string buttonValue, string buttonName);
        /// <summary>
        /// Add form button
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        /// <param name="buttonId"></param>
        public abstract void AddFormButton(string buttonValue, string buttonName, string buttonId);
        /// <summary>
        /// Add form button
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        /// <param name="buttonId"></param>
        /// <param name="buttonClass"></param>
        public abstract void AddFormButton(string buttonValue, string buttonName, string buttonId, string buttonClass);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// This report will be wrapped in a form tag and the action should send traffic back to the same page. If empty, the form uses cp.Doc.RefreshQueryString
        /// </summary>
        [Obsolete("Deprecated. No longer needed.", false)] public abstract string FormActionQueryString { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        public abstract string FormId { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The main html of the tool form
        /// </summary>
        public abstract string Body { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// html block below the body
        /// </summary>
        public abstract string Footer { get; set; }
        // 
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Method retrieves the rendered html. Call this method after populating all object elements
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use getHtml()", false)] public abstract string GetHtml(CPBaseClass cp);
    }
}
