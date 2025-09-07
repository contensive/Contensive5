
using System;

namespace Contensive.Processor {
    /// <summary>
    /// Searialize and deserialize. Recommend because Newtonsoft is a very popular package, and an addon may need it, and include a nuiget that needs a different version. 
    /// This issue is mitigated by letting our methods call this interface.
    /// </summary>
    public class CPJSONClass : BaseClasses.CPJSONBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Serialize an object to a json string
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override string Serialize(object obj) {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Deserialize a string to a known object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="JSON"></param>
        /// <returns></returns>
        public override T Deserialize<T>(string JSON) {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(JSON);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Deserialize a string to an unknown object
        /// </summary>
        /// <param name="JSON"></param>
        /// <returns></returns>
        public override object Deserialize(string JSON) {
            return Newtonsoft.Json.JsonConvert.DeserializeObject(JSON);
        }
    }
}