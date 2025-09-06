
using System;

namespace Contensive.BaseClasses {
    //
    //====================================================================================================
    /// <summary>
    /// CP.Block - an object that holds and manipulates a block of html
    /// </summary>
    /// <remarks></remarks>
    public abstract class CPBlockBaseClass : IDisposable {
        //
        //====================================================================================================
        /// <summary>
        /// Load the block with an html string
        /// </summary>
        /// <param name="htmlString"></param>
        /// <remarks></remarks>
        public abstract void Load(string htmlString);
        //
        //====================================================================================================
        /// <summary>
        /// load the block with the entire contents of a file in the wwwRoot
        /// </summary>
        /// <param name="wwwFileName"></param>
        /// <remarks></remarks>
        public abstract void OpenFile(string wwwFileName);
        //
        //====================================================================================================
        /// <summary>
        /// load the block with the contents of a record in Copy Content
        /// </summary>
        /// <param name="copyRecordName"></param>
        /// <remarks></remarks>
        public abstract void OpenCopy(string copyRecordName);
        //
        //====================================================================================================
        /// <summary>
        /// load the block with the contents of a record in Layouts
        /// </summary>
        /// <param name="layoutRecordName"></param>
        /// <remarks></remarks>
        public abstract void OpenLayout(string layoutRecordName);
        //
        //====================================================================================================
        /// <summary>
        /// load the block with the body of a file in the wwwRoot
        /// </summary>
        /// <param name="wwwFileName"></param>
        /// <remarks></remarks>
        public abstract void ImportFile(string wwwFileName);
        //
        //====================================================================================================
        /// <summary>
        /// set the innerHtml of an element in the current block specified by the findSelector
        /// </summary>
        /// <param name="findSelector"></param>
        /// <param name="htmlString"></param>
        /// <remarks></remarks>
        public abstract void SetInner(string findSelector, string htmlString);
        //
        //====================================================================================================
        /// <summary>
        /// Return the innerHtml from the current block specified by the findSelector
        /// </summary>
        /// <param name="findSelector"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetInner(string findSelector);
        //
        //====================================================================================================
        /// <summary>
        /// Set the OuterHtml in the current block specified by the findSelector to the htmlString
        /// </summary>
        /// <param name="findSelector"></param>
        /// <param name="htmlString"></param>
        /// <remarks></remarks>
        public abstract void SetOuter(string findSelector, string htmlString);
        //
        //====================================================================================================
        /// <summary>
        /// return the outer Html specified by the findSelector
        /// </summary>
        /// <param name="findSelector"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetOuter(string findSelector);
        //
        //====================================================================================================
        /// <summary>
        /// append the htmlString into the current Block
        /// </summary>
        /// <param name="htmlString"></param>
        /// <remarks></remarks>
        public abstract void Append(string htmlString);
        //
        //====================================================================================================
        /// <summary>
        /// Prepend the htmlString on the current block
        /// </summary>
        /// <param name="htmlString"></param>
        /// <remarks></remarks>
        public abstract void Prepend(string htmlString);
        //
        //====================================================================================================
        /// <summary>
        /// return the entire html of the current block
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetHtml();
        //
        //====================================================================================================
        /// <summary>
        ///  Clear the contents of the current block
        /// </summary>
        /// <remarks></remarks>
        public abstract void Clear();
        //
        //====================================================================================================
        /// <summary>
        /// support IDisposable
        /// </summary>
        public abstract void Dispose();
    }
}


