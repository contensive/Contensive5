
using System;
using System.Collections.Generic;
//
namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// contains the hash of a cache key. Class created only to use types to help unwind complicated argument patterns
    /// </summary>
    public class CacheKeyHashClass {
        //
        // ====================================================================================================
        /// <summary>
        /// The hashed key
        /// </summary>
        public string hash { get; set; }
        /// <summary>
        /// The original key before hashing
        /// </summary>
        public string key { get; set; }
    }
}