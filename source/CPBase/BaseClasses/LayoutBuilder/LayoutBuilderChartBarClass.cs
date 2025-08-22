
// instead of a chart pie, bar, line page, just implement the charts and they can be used in a list layout above-body section


//using System;

//namespace Contensive.BaseClasses.LayoutBuilder {
//    public abstract class LayoutBuilderChartBarClass(CPBaseClass cp) {
//        public CPBaseClass cp { get; set; } = cp;
//        //
//        //-------------------------------------------------
//        //
//        public abstract bool includeBodyPadding { get; set; }
//        //
//        //-------------------------------------------------
//        //
//        public abstract bool includeBodyColor { get; set; }
//        //
//        //-------------------------------------------------
//        //
//        public abstract string yAxisCaption { get; set; }
//        //
//        //-------------------------------------------------
//        //
//        //-------------------------------------------------
//        //
//        public abstract string xAxisCaption { get; set; }
//        //
//        //-------------------------------------------------
//        //
//        //-------------------------------------------------
//        //
//        public abstract bool isOuterContainer { get; set; }
//        //
//        //-------------------------------------------------
//        //
//        //-------------------------------------------------
//        //
//        public abstract string styleSheet { get; set; }
//        //
//        //-------------------------------------------------
//        //
//        //-------------------------------------------------
//        //
//        public abstract string javascript { get; set; }
//        //
//        // ====================================================================================================
//        /// <summary>
//        /// Optional. If set, this value will populate the title in the subnav of the portalbuilder
//        /// </summary>
//        public abstract List,string> portalSubNavTitleList { get; set; }
//        //
//        //-------------------------------------------------
//        // getResult
//        //-------------------------------------------------
//        //
//        public abstract string getHtml(CPBaseClass cp);
//        //
//        //-------------------------------------------------
//        // add html blocks
//        //-------------------------------------------------
//        //
//        public abstract string htmlLeftOfTable { get; set; }
//        //
//        public abstract string htmlBeforeTable { get; set; }
//        //
//        public abstract string htmlAfterTable { get; set; }
//        //
//        //-------------------------------------------------
//        // add a form hidden
//        //-------------------------------------------------
//        //
//        public abstract void addFormHidden(string Name, string Value);
//        //
//        public abstract void addFormHidden(string name, int value);
//        //
//        public abstract void addFormHidden(string name, double value);
//        //
//        public abstract void addFormHidden(string name, DateTime value);
//        //
//        public abstract void addFormHidden(string name, bool value);
//        //
//        //-------------------------------------------------
//        // add a form button
//        //-------------------------------------------------
//        //
//        public abstract void addFormButton(string buttonValue);
//        public abstract void addFormButton(string buttonValue, string buttonName);
//        public abstract void addFormButton(string buttonValue, string buttonName, string buttonId);
//        public abstract void addFormButton(string buttonValue, string buttonName, string buttonId, string buttonClass);
//        //
//        //-------------------------------------------------
//        // setForm
//        //-------------------------------------------------
//        //
//        public abstract string formActionQueryString { get; set; }
//        public abstract string formId { get; set; }
//        //
//        //-------------------------------------------------
//        // Refresh Query String
//        //-------------------------------------------------
//        //
//        //public abstract string refreshQueryString
//        //{
//        //    get
//        //    {
//        //        return localFrameRqs;
//        //    }
//        //    set
//        //    {
//        //        localFrameRqs = value;
//        //    }
//        //}
//        //
//        //-------------------------------------------------
//        // Title
//        //-------------------------------------------------
//        //
//        public abstract string title { get; set; }
//        //
//        //-------------------------------------------------
//        // Warning
//        //-------------------------------------------------
//        //
//        public abstract string warning { get; set; }
//        //
//        //-------------------------------------------------
//        // Description
//        //-------------------------------------------------
//        //
//        public abstract string description { get; set; }
//        //
//        //-------------------------------------------------
//        // column properties
//        //-------------------------------------------------
//        //
//        public abstract string columnCaption { get; set; }
//        //
//        //-------------------------------------------------
//        // set the caption class for this column
//        //-------------------------------------------------
//        //
//        public abstract string columnCaptionClass { get; set; }
//        //
//        //-------------------------------------------------
//        // set the cell class for this column
//        //-------------------------------------------------
//        //
//        public abstract string columnCellClass { get; set; }
//        //
//        //-------------------------------------------------
//        // add a column
//        //-------------------------------------------------
//        //
//        public abstract void addColumn();
//        //
//        //-------------------------------------------------
//        // row Caption for grid
//        //-------------------------------------------------
//        //
//        public abstract string rowCaption { get; set; }
//        //
//        //-------------------------------------------------
//        // row Caption Class for grid
//        //-------------------------------------------------
//        //
//        public abstract string rowCaptionClass { get; set; }
//        //
//        //-------------------------------------------------
//        // chart width
//        //-------------------------------------------------
//        //
//        public abstract int chartWidth { get; set; }
//        //
//        //-------------------------------------------------
//        // chart height
//        //-------------------------------------------------
//        //
//        public abstract int chartHeight { get; set; }
//        //
//        //-------------------------------------------------
//        // add a row
//        //-------------------------------------------------
//        //
//        public abstract void addRow();
//        //
//        //-------------------------------------------------
//        // populate a cell
//        //-------------------------------------------------
//        //
//        public abstract void setCell(int barHeight, string clickLink);
//        public abstract void setCell(int barHeight);
//    }
//}