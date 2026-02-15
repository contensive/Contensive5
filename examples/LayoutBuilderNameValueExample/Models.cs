using Contensive.BaseClasses;
using System;
using System.Collections.Generic;

namespace Contensive.AccountBilling.Models.Db {
    /// <summary>
    /// Base class for all database models
    /// </summary>
    public class DbBaseModel {
        public int id { get; set; }
        public string name { get; set; }

        /// <summary>
        /// Generic factory method to create model instances from the database
        /// </summary>
        public static T create<T>(CPBaseClass cp, int recordId) where T : DbBaseModel, new() {
            return new T() { id = recordId };
        }
    }

    /// <summary>
    /// Order/Invoice model representing a customer order
    /// </summary>
    public class OrderModel : DbBaseModel {
        public int accountId { get; set; }
        public double totalCharge { get; set; }
        public DateTime dateAdded { get; set; }
        public DateTime dateDue { get; set; }
        public DateTime? dateCompleted { get; set; }
        public DateTime? dateCanceled { get; set; }
        public DateTime? payDate { get; set; }
    }

    /// <summary>
    /// Account model representing a customer account
    /// </summary>
    public class AccountModel : DbBaseModel {
        public int payMethodID { get; set; }
        public bool closed { get; set; }
    }

    /// <summary>
    /// Account auto-pay options including credit cards and gift cards
    /// </summary>
    public class AccountAutoPayOptionModel : DbBaseModel {
        public int accountId { get; set; }
        public string ccNumber { get; set; }
        public string ccNumberEncryptedLastFour { get; set; }
        public DateTime ccExpiration { get; set; }
        public double giftCardOriginalAmount { get; set; }
        public double giftCardAmountSpent { get; set; }
        public bool isGiftCard { get; set; }

        /// <summary>
        /// Get list of gift cards for an account
        /// </summary>
        public static List<AccountAutoPayOptionModel> getGiftCardList(CPBaseClass cp, int accountId) {
            return new List<AccountAutoPayOptionModel>();
        }

        /// <summary>
        /// Get count of credit cards on file for an account
        /// </summary>
        public static int getCreditCardCount(CPBaseClass cp, int accountId) {
            return 0;
        }

        /// <summary>
        /// Get list of credit cards and ACH methods for an account
        /// </summary>
        public static List<AccountAutoPayOptionModel> getCreditCardACHList(CPBaseClass cp, int accountId) {
            return new List<AccountAutoPayOptionModel>();
        }
    }

    /// <summary>
    /// Ecommerce export model for reporting
    /// </summary>
    public class EcommerceExportModel {
        /// <summary>
        /// Get the start date of the next accounting period
        /// </summary>
        public static DateTime getNextPeriodStartDate(CPBaseClass cp) {
            return DateTime.MinValue;
        }
    }
}
