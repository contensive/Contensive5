﻿
namespace Contensive.BaseClasses {
    /// <summary>
    /// provate consistent JSON methods in a single updateable layer
    /// </summary>
    public abstract class CPJSONBaseClass {
        //
        //==========================================================================================
        /// <summary>
        /// Serialize an object to JSON
        /// </summary>
        /// <param name="obj"></param>
        public abstract string Serialize(object obj);
        //
        //==========================================================================================
        /// <summary>
        /// Deserialize a
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="JSON"></param>
        /// <returns></returns>
        public abstract T Deserialize<T>(string JSON);
    }
}

