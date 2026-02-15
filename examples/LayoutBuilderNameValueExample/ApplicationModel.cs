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
        /// Current date/time for the application
        /// </summary>
        public DateTime rightNow { get; set; }

        /// <summary>
        /// Site-level properties and configuration
        /// </summary>
        public SitePropertyController siteProperties { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cp">The CPBaseClass instance</param>
        public ApplicationModel(CPBaseClass cp) {
            this.cp = cp;
            this.rightNow = DateTime.Now;
            this.siteProperties = new SitePropertyController();
        }

        /// <summary>
        /// Dispose implementation for using statement support
        /// </summary>
        public void Dispose() {
            // Cleanup resources
        }
    }

    /// <summary>
    /// Site-level property controller
    /// </summary>
    public class SitePropertyController {
        public enum PaymentProcessMethodEnum {
            NoProcessorAvailableForceBilling,
            StripeCheckout,
            Other
        }

        public PaymentProcessMethodEnum paymentProcessMethod { get; set; }
        public bool creditCardEncryption { get; set; }
    }
}
