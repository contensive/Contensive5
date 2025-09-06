
using System.Collections.Generic;

namespace Contensive.BaseModels {
    //
    /// <summary>
    /// case insensative dictionary. Use for application lookup
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <typeparam name="S"></typeparam>
    public abstract class CaseInsensitiveDictionaryBaseModel<S, V> : Dictionary<string, V> {
    }
}