
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
            try {
                return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            } catch (Exception ex) {
                string typeName = obj == null ? "(null)" : obj.GetType().FullName;
                throw new InvalidOperationException($"CPJSONClass.Serialize error, object type [{typeName}]", ex);
            }
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
            try {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(JSON);
            } catch (Exception ex) {
                string preview = string.IsNullOrEmpty(JSON) ? "(null or empty)" : JSON.Substring(0, Math.Min(JSON.Length, 100));
                throw new InvalidOperationException($"CPJSONClass.Deserialize<{typeof(T).Name}> error, JSON preview [{preview}]", ex);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Deserialize a string to an unknown object
        /// </summary>
        /// <param name="JSON"></param>
        /// <returns></returns>
        public override object Deserialize(string JSON) {
            try {
                return Newtonsoft.Json.JsonConvert.DeserializeObject(JSON);
            } catch (Exception ex) {
                string preview = string.IsNullOrEmpty(JSON) ? "(null or empty)" : JSON.Substring(0, Math.Min(JSON.Length, 100));
                throw new InvalidOperationException($"CPJSONClass.Deserialize error, JSON preview [{preview}]", ex);
            }
        }
    }
}