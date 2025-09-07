
using System;
using System.Collections.Generic;
//
namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// cache document that holds the data object and other values used for control
    /// </summary>
    //
    public class CacheDocumentClass {
        //
        // ====================================================================================================
        //
        public CacheDocumentClass(DateTime dateTimeMockable)  {
            dependentKeyHashList = new List<CacheKeyHashClass>();
            saveDate = dateTimeMockable;
            invalidationDate = dateTimeMockable.AddDays(Constants.invalidationDaysDefault);
        }
        //
        // if populated, all other properties are ignored and the primary tag b
        public CacheKeyHashClass keyPtrHash;
        //
        // this object is invalidated if any of these objects are invalidated
        public List<CacheKeyHashClass> dependentKeyHashList;
        //
        // the date this object was last saved.
        public DateTime saveDate;
        //
        // the future date when this object self-invalidates
        public DateTime invalidationDate;
        //
        // the data storage
        public object content;
    }
}