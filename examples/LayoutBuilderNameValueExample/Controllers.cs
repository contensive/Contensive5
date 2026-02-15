using Contensive.AccountBilling.Models.Db;
using Contensive.AccountBilling.Models.Domain;
using Contensive.BaseClasses;
using System;
using System.Collections.Generic;

namespace Contensive.AccountBilling.Controllers {
    /// <summary>
    /// Controller for handling security and authorization
    /// </summary>
    public static class SecurityController {
        public static string getNotAuthorizedHtmlResponse(CPBaseClass cp) {
            return "<h1>Not Authorized</h1><p>You do not have permission to access this resource.</p>";
        }
    }

    /// <summary>
    /// Controller for handling portal features and navigation
    /// </summary>
    public static class PortalController {
        public static string RedirectToPortalFeature(CPBaseClass cp, string portalFeatureGuid) {
            return "";
        }
    }

    /// <summary>
    /// Helper controller for HTML generation
    /// </summary>
    public static class HtmlController {
        public static string width400px(string content, string cssClass = "") {
            return $"<div style=\"width: 400px;\" class=\"{cssClass}\">{content}</div>";
        }

        public static string width100px(string content) {
            return $"<div style=\"width: 100px;\">{content}</div>";
        }

        public static string wrapFormCheck(string content) {
            return $"<div class=\"form-check\">{content}</div>";
        }

        public static string wrapFieldHelp(string helpText) {
            return $"<small class=\"form-text text-muted\">{helpText}</small>";
        }

        public static string getInputRadio(CPBaseClass cp, string name, string value, string currentValue, string cssClass, string id) {
            string checkedAttr = (value == currentValue) ? " checked" : "";
            return $"<input type=\"radio\" name=\"{name}\" value=\"{value}\" class=\"{cssClass}\" id=\"{id}\"{checkedAttr}>";
        }
    }

    /// <summary>
    /// Generic helper controller
    /// </summary>
    public static class GenericController {
        public static string getFirstName(string fullName) {
            if (string.IsNullOrEmpty(fullName)) return "";
            var parts = fullName.Split(' ');
            return parts.Length > 0 ? parts[0] : "";
        }

        public static string getLastName(string fullName) {
            if (string.IsNullOrEmpty(fullName)) return "";
            var parts = fullName.Split(' ');
            return parts.Length > 1 ? parts[parts.Length - 1] : "";
        }

        public static string encodeShortDateString(DateTime date) {
            return date.ToShortDateString();
        }
    }

    /// <summary>
    /// Processing controller for payment operations
    /// </summary>
    public static class ProcessingController {
        public static string getSqlCriteriaUnappliedPayments(int accountId) {
            return $"(accountId={accountId})and(applied=0)";
        }
    }

    /// <summary>
    /// General Ledger controller
    /// </summary>
    public static class GeneralLedgerController {
        public static int verifyGLARAccountId(CPBaseClass cp, int accountId) {
            return accountId;
        }

        public static void createChargeDebitCredit(CPBaseClass cp, int orderId, ref string userError, DateTime transactionDate, int glAccountId) {
            // Implementation would create GL entries
        }
    }

    /// <summary>
    /// Credit card processing controller
    /// </summary>
    public static class CreditCardController {
        public static void validatePayOptionEncryption(ApplicationModel app, AccountAutoPayOptionModel payOption) {
            // Implementation would validate encryption
        }

        public static string getLastFour(string cardNumber) {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4) return "";
            return cardNumber.Substring(cardNumber.Length - 4);
        }
    }

    /// <summary>
    /// Workflow controller for payment processing
    /// </summary>
    public static class WorkflowController {
        public static bool workflow_processOrderPayment(ApplicationModel app, ref string userError, ProcessPaymentRequest request) {
            // Implementation would process the payment workflow
            return true;
        }

        public static int processPaymentWorkflow_createCreditCardPaymentTransactions(ApplicationModel app, ProcessPaymentsWorkflowRequest request) {
            return 0;
        }

        public static void processPaymentWorkflow_createOverpaymentDepositTransaction(ApplicationModel app, ProcessPaymentsWorkflowRequest request) {
            // Implementation would create overpayment deposit
        }

        public static void processPaymentWorkflow_createGiftcardDepositsForMultiSourceTransaction(ApplicationModel app, ProcessPaymentsWorkflowRequest request) {
            // Implementation would create giftcard deposits
        }

        public static void processPaymentWorkflow_applyMultipleDepositsToOrder(ApplicationModel app, ProcessPaymentsWorkflowRequest request) {
            // Implementation would apply deposits
        }

        public static void workflowStep_recordTransactionsStep(ApplicationModel app, DateTime transactionDate, List<OrderModel> orderList, int paymentTransactionId, ref string userError, int glAccountIdDeposits, string paymentReferenceNumber, PayMethodEnum payMethod, AccountModel account, bool blockReceipt) {
            // Implementation would record transactions
        }

        public static void workflowStep_notification(ApplicationModel app, List<OrderModel> orderList, AccountModel account, bool blockReceipt) {
            // Implementation would send notifications
        }

        public static void workflowStep_fulfillOnPaymentStep(ApplicationModel app, List<OrderModel> orderList, AccountModel account) {
            // Implementation would fulfill orders
        }

        // Enums and supporting classes
        public enum TransactionPaymentTypeEnum {
            StripeCheckout,
            Cash,
            Check,
            CreditCard
        }

        public enum PayMethodEnum {
            Check = 1,
            CreditCard = 2,
            Deposit = 3,
            Cash = 4,
            CreditCardOnFile = 5,
            CreditCardStripeCheckout = 6
        }

        /// <summary>
        /// Request object for payment workflow processing
        /// </summary>
        public class ProcessPaymentsWorkflowRequest {
            public AccountModel account { get; set; }
            public List<OrderModel> orderList { get; set; }
            public double cashCheckOrCardAmount { get; set; }
            public DateTime requestPaymentTransactionDate { get; set; }
            public List<int> paymentSource_Deposits { get; set; }
            public CPBaseClass cp { get; set; }
            public double orderTotal { get; set; }
            public string invoiceListCaption { get; set; }
            public bool hasMultiplePaymentSources { get; set; }
            public int glAccountIdCash { get; set; }
            public string paymentReferenceNumber { get; set; }
            public string paymentAuthMessage { get; set; }
            public TransactionPaymentTypeEnum transactionPaymentType { get; set; }
            public int payOption { get; set; }
            public string return_userError { get; set; }
            public double giftCardAmountToApply { get; set; }
            public int hint { get; set; }
            public bool paymentApproved { get; set; }
            public List<int> paymentSource_GiftCards { get; set; }
            public bool blockReceipt { get; set; }
        }
    }

    /// <summary>
    /// Stripe checkout payment controller
    /// </summary>
    public static class StripeCheckoutPaymentController {
        public static CheckoutResult checkoutPhase2_processCheckoutSession(ApplicationModel app, string stripeEchoEncoded) {
            return new CheckoutResult();
        }

        public class CheckoutResult {
            public bool success { get; set; }
            public StripeEcho stripeEcho { get; set; }
            public string paymentReferenceNumber { get; set; }
            public string paymentAuthMessage { get; set; }
            public int payOption { get; set; }
        }

        public class StripeEcho {
            public int accountId { get; set; }
            public List<int> orderIdList { get; set; }
            public double amount { get; set; }
            public DateTime requestPaymentTransactionDate { get; set; }
            public List<int> paymentSource_Deposits { get; set; }
            public double orderTotal { get; set; }
            public string invoiceListCaption { get; set; }
            public bool hasMultiplePaymentSources { get; set; }
            public double giftCardAmountToApply { get; set; }
            public List<int> paymentSource_GiftCards { get; set; }
            public bool blockReceipt { get; set; }
        }
    }

    /// <summary>
    /// Payment form controller namespace
    /// </summary>
    public static class OnlinePaymentFormController {
        public struct OnDemandMethodStruct {
            public string creditCardNumber { get; set; }
            public string creditCardExpiration { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string SecurityCode { get; set; }
            public bool useAch { get; set; }
            public string Zip { get; set; }
        }
    }

    /// <summary>
    /// API request structure for payment processing
    /// </summary>
    public class ProcessPaymentApiRequest {
        public enum PaymentMethodEnum {
            Check = 1,
            CreditCard = 2,
            Deposit = 3,
            Cash = 4,
            CreditCardOnFile = 5,
            CreditCardStripeCheckout = 6
        }
    }

    /// <summary>
    /// Request object for processing payment
    /// </summary>
    public class ProcessPaymentRequest {
        public bool blockReceipt { get; set; }
        public bool createDeposit { get; set; }
        public int accountId { get; set; }
        public int paymentMethod { get; set; }
        public bool allowAdjustment { get; set; }
        public DateTime paymentTransactionDate { get; set; }
        public List<OrderModel> orderList { get; set; }
        public double paymentAmount { get; set; }
        public string checkNumber { get; set; }
        public string paymentMemo { get; set; }
        public int creditCardOnFileId { get; set; }
        public OnlinePaymentFormController.OnDemandMethodStruct paymentCreditCard { get; set; }
        public List<int> giftcardIdList { get; set; } = new List<int>();
        public List<int> unappliedDepositIdList { get; set; }
        public string stripeReturnUrl { get; set; }
    }
}
