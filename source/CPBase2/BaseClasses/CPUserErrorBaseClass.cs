
namespace Contensive.BaseClasses {
    /// <summary>
    /// Can be used to accumulate user errors during a disperate process. 
    /// </summary>
    public abstract class CPUserErrorBaseClass {
        /// <summary>
        /// Add an error to the list
        /// </summary>
        /// <param name="message"></param>
        public abstract void Add(string message);
        /// <summary>
        /// get the list of errors
        /// </summary>
        /// <returns></returns>
        public abstract string GetList();
        /// <summary>
        /// returns true if there are no errors
        /// </summary>
        /// <returns></returns>
        public abstract bool OK();
        //
        //====================================================================================================
        // deprecated
        //
    }
}

