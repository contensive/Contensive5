namespace Contensive.AccountBilling {
    /// <summary>
    /// Application constants used throughout the Account Billing addon
    /// </summary>
    public static class Constants {
        // ====================================================================================================
        // -- Form ID constants
        // ====================================================================================================

        /// <summary>Form ID for the payment tool form</summary>
        public const int formIdToolPayment = 2001;

        // ====================================================================================================
        // -- Request name constants (form field names)
        // ====================================================================================================

        public const string rnButton = "button";
        public const string rnAccountId = "accountId";
        public const string rnSrcFormId = "srcFormId";
        public const string rnFrameRqs = "frameRqs";
        public const string rnDstFeatureGuid = "dstFeatureGuid";

        // Payment form fields
        public const string rnPayFormPickAccountId = "payFormPickAccountId";
        public const string rnPayFormPickPayMethodId = "payFormPickPayMethodId";
        public const string rnPayFormPickOrderId = "payFormPickOrderId";
        public const string rnPayMethodId = "payMethodId";
        public const string rnInvoiceCnt = "invoiceCnt";
        public const string rnPaymentInvoice = "paymentInvoice";
        public const string rnPaymentDeposit = "paymentDeposit";
        public const string rnPaymentGiftCard = "paymentGiftCard";

        // Payment amounts
        public const string rnPaymentAmountCash = "paymentAmountCash";
        public const string rnPaymentAmountCheck = "paymentAmountCheck";
        public const string rnPaymentAmountCreditCard = "paymentAmountCreditCard";
        public const string rnPaymentAmountCreateDeposit = "paymentAmountCreateDeposit";

        // Payment details
        public const string rnPaymentMemo = "paymentMemo";
        public const string rncheckNumber = "checkNumber";
        public const string rnPaymentBlockReceipt = "paymentBlockReceipt";
        public const string rnPaymentCreatedeposit = "paymentCreateDeposit";
        public const string rnPaymentTransactionDate = "paymentTransactionDate";
        public const string rnAllowAdjustment = "allowAdjustment";

        // Credit card fields
        public const string rnCreditCardNumber = "creditCardNumber";
        public const string rnCreditCardMonth = "creditCardMonth";
        public const string rnCreditCardYear = "creditCardYear";
        public const string rnCreditCardName = "creditCardName";
        public const string rnCreditCardCVV = "creditCardCVV";
        public const string rnCreditCardZip = "creditCardZip";

        // ====================================================================================================
        // -- Button constants
        // ====================================================================================================

        public const string buttonCancel = "Cancel";
        public const string buttonProcessPayment = "Process Payment";

        // ====================================================================================================
        // -- GUID constants
        // ====================================================================================================

        public const string guidPortalFeaturePay = "{AAAABBBB-1111-2222-3333-CCCCDDDDEEEE}";
        public const string guidPortalAccountManager = "{87654321-4321-4321-4321-CBA987654321}";

        // ====================================================================================================
        // -- Content name constants
        // ====================================================================================================

        public const string cnAccounts = "Accounts";
    }
}
