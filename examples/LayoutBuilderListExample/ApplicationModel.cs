using Contensive.BaseClasses;
using System;

namespace Contensive.AccountBilling.Models.Domain {
    /// <summary>
    /// Application model that wraps the CPBaseClass instance
    /// Provides application-level context and utilities
    /// </summary>
    public class ApplicationModel : IDisposable {
        /// <summary>
        /// The CPBaseClass instance for accessing Contensive functionality
        /// </summary>
        public CPBaseClass cp { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cp">The CPBaseClass instance</param>
        public ApplicationModel(CPBaseClass cp) {
            this.cp = cp;
        }

        /// <summary>
        /// Dispose implementation for using statement support
        /// </summary>
        public void Dispose() {
            // Cleanup resources
        }
    }
}
