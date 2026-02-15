using System;
using System.Collections.Generic;
using System.Linq;
using Contensive.AccountBilling.Controllers;
using Contensive.AccountBilling.Models.Db;
using Contensive.AccountBilling.Models.Domain;
using Contensive.BaseClasses;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Stripe;
using Stripe.Forwarding;
using static Contensive.AccountBilling.Controllers.OnlinePaymentFormController;
using static Contensive.AccountBilling.Controllers.WorkflowController;
// -- Imports Contensive.Models.Db

namespace Contensive.AccountBilling {
    //
    // ====================================================================================================
    //
    public class AccountManagerToolPaymentAddon : AddonBaseClass {
        //
        // ====================================================================================================
        /// <summary>
        /// addon to create and process payment tool
        /// </summary>
        /// <param name="CP"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass CP) {
            try {
                using var app = new ApplicationModel(CP);
                //
                // -- authenticate/authorize
                if (!CP.User.IsAdmin) { return SecurityController.getNotAuthorizedHtmlResponse(CP); }
                //
                // -- validate portal environment
                if (!CP.AdminUI.EndpointContainsPortal()) { return PortalController.RedirectToPortalFeature(CP, Constants.guidPortalFeaturePay); }
                //
                string userError = "";
                int dstFormId = processForm(app, ref userError);
                if (dstFormId == Constants.formIdToolPayment) {
                    return getForm(app);
                }
                //
                // -- if not handled, return blank which is the signal for return to the parent addon
                return "";
            } catch (Exception ex) {
                CP.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        // ====================================================================================================
        //
        //
        public int processForm(ApplicationModel app, ref string return_userError) {
            var cp = app.cp;
            try {
                //
                // -- stripe checkout, pass2, stripe returned
                string stripeEchoEncoded = app.cp.Doc.GetText("stripeEcho");
                if (app.siteProperties.paymentProcessMethod.Equals(SitePropertyController.PaymentProcessMethodEnum.StripeCheckout) && !string.IsNullOrEmpty(stripeEchoEncoded)) {
                    //
                    // -- complete the stripe checkout workflow
                    // -- stripe-checkout pass-2, api payment case
                    // -- if pass-2 of stripe-checkout, complete the order
                    // -- in stripe-checkout, during pass-1 the order is checked and the user is forwarded to the stripe-checkout site
                    // -- when the customer completes the payment on stripe they are returned here. For the onlineCheckoutform, it will enter with no orderList because this is not the form they submitted
                    // -- read success or fail from the stripeecho in the url and return as result of payment
                    //
                    app.cp.Log.Debug($"processForm, stripe-checkout pass-2, stripeEcho [{stripeEchoEncoded}]");
                    //
                    var checkoutResult = StripeCheckoutPaymentController.checkoutPhase2_processCheckoutSession(app, stripeEchoEncoded);
                    if (!checkoutResult.success) {
                        //
                        app.cp.Log.Debug($"checkout_intercept (string-checkout), pass-2 fail");
                        //
                        return Constants.formIdToolPayment;
                    }
                    app.cp.Log.Debug($"checkout_intercept (string-checkout), pass-2, success, order paid");
                    //
                    AccountModel orderAccount = DbBaseModel.create<AccountModel>(app.cp, checkoutResult.stripeEcho.accountId);
                    var orderList = new List<OrderModel>();
                    foreach (var orderId in checkoutResult.stripeEcho.orderIdList) {
                        orderList.Add(DbBaseModel.create<OrderModel>(app.cp, orderId));
                    }
                    //
                    // -- payment approved, handle notifications and fulfillment
                    var transactionWorkflowRequest = new ProcessPaymentsWorkflowRequest {
                        account = orderAccount,
                        orderList = orderList,
                        cashCheckOrCardAmount = checkoutResult.stripeEcho.amount,
                        requestPaymentTransactionDate = app.rightNow,
                        paymentSource_Deposits = checkoutResult.stripeEcho.paymentSource_Deposits,
                        cp = cp,
                        orderTotal = checkoutResult.stripeEcho.orderTotal,
                        invoiceListCaption = checkoutResult.stripeEcho.invoiceListCaption,
                        hasMultiplePaymentSources = checkoutResult.stripeEcho.hasMultiplePaymentSources,
                        glAccountIdCash = 0,
                        paymentReferenceNumber = checkoutResult.paymentReferenceNumber,
                        paymentAuthMessage = checkoutResult.paymentAuthMessage,
                        transactionPaymentType = TransactionPaymentTypeEnum.StripeCheckout,
                        payOption = checkoutResult.payOption
                    };
                    var paymentTransactionId = WorkflowController.processPaymentWorkflow_createCreditCardPaymentTransactions(app, transactionWorkflowRequest);
                    //
                    WorkflowController.processPaymentWorkflow_createOverpaymentDepositTransaction(app, transactionWorkflowRequest);
                    //
                    var otherTransWorklflowRequest = new ProcessPaymentsWorkflowRequest {
                        account = orderAccount,
                        orderList = orderList,
                        requestPaymentTransactionDate = app.rightNow,
                        paymentSource_Deposits = checkoutResult.stripeEcho.paymentSource_Deposits,
                        cp = cp,
                        orderTotal = checkoutResult.stripeEcho.orderTotal,
                        invoiceListCaption = checkoutResult.stripeEcho.invoiceListCaption,
                        hasMultiplePaymentSources = checkoutResult.stripeEcho.hasMultiplePaymentSources,
                        glAccountIdCash = 0,
                        return_userError = "",
                        giftCardAmountToApply = checkoutResult.stripeEcho.giftCardAmountToApply,
                        hint = 0,
                        paymentApproved = true,
                        paymentSource_GiftCards = checkoutResult.stripeEcho.paymentSource_GiftCards
                    };
                    //
                    WorkflowController.processPaymentWorkflow_createGiftcardDepositsForMultiSourceTransaction(app, otherTransWorklflowRequest);
                    //
                    processPaymentWorkflow_applyMultipleDepositsToOrder(app, otherTransWorklflowRequest);
                    //
                    // -- do all the order payment workflow steps
                    int generalLedgerAccountID = GeneralLedgerController.verifyGLARAccountId(app.cp, 0);
                    GeneralLedgerController.createChargeDebitCredit(app.cp, orderList?.First()?.id ?? 0, ref return_userError, app.rightNow, generalLedgerAccountID);
                    //
                    int glAccountIdDeposits = 0;
                    PayMethodEnum payMethod = (PayMethodEnum)orderAccount.payMethodID;
                    WorkflowController.workflowStep_recordTransactionsStep(app, checkoutResult.stripeEcho.requestPaymentTransactionDate, orderList, paymentTransactionId, ref return_userError, glAccountIdDeposits, checkoutResult.paymentReferenceNumber, payMethod, orderAccount, checkoutResult.stripeEcho.blockReceipt);
                    //
                    // -- send receipt to buyer
                    WorkflowController.workflowStep_notification(app, orderList, orderAccount, checkoutResult.stripeEcho.blockReceipt);
                    //
                    WorkflowController.workflowStep_fulfillOnPaymentStep(app, orderList, orderAccount);
                    //
                    // -- set flag for form display
                    cp.Doc.SetProperty("paymentApproved", true);
                    cp.Doc.SetProperty(Constants.rnAccountId, orderAccount.id);
                    //
                    return Constants.formIdToolPayment;
                }
                //
                DateTime requestPaymentTransactionDate = cp.Doc.GetDate(Constants.rnPaymentTransactionDate);
                var request = new ProcessPaymentRequest() {
                    blockReceipt = cp.Doc.GetBoolean(Constants.rnPaymentBlockReceipt),
                    createDeposit = cp.Doc.GetBoolean(Constants.rnPaymentCreatedeposit),
                    accountId = cp.Doc.GetInteger(Constants.rnAccountId),
                    paymentMethod = cp.Doc.GetInteger(Constants.rnPayMethodId),
                    allowAdjustment = cp.Doc.GetBoolean(Constants.rnAllowAdjustment),
                    paymentTransactionDate = requestPaymentTransactionDate == DateTime.MinValue ? app.rightNow : requestPaymentTransactionDate
                };
                //
                // -- if nothing to process, display form
                int srcFormId = cp.Doc.GetInteger(Constants.rnSrcFormId);
                if (srcFormId == 0) { return Constants.formIdToolPayment; }
                //
                // -- handle cancel button first
                if ((cp.Doc.GetText(Constants.rnButton) ?? "") == Constants.buttonCancel) {
                    if (request.accountId != 0) {
                        //
                        // -- if not he first page, go to first page
                        cp.Doc.SetProperty(Constants.rnAccountId, 0);
                        return Constants.formIdToolPayment;
                    }
                    //
                    // -- else ecommerce default
                    return 0;
                }
                //
                // -- handle the settings picked while determining the type of payment to process
                if (cp.Doc.GetInteger(Constants.rnPayFormPickAccountId) > 0) {
                    //
                    // picking account
                    //
                    return Constants.formIdToolPayment;
                }
                if (cp.Doc.GetInteger(Constants.rnPayFormPickPayMethodId) > 0) {
                    //
                    // picking pay method
                    //
                    return Constants.formIdToolPayment;
                }
                //
                // -- create orderidlist
                //
                request.orderList = [];
                for (int orderPtr = 0, loopTo = cp.Doc.GetInteger(Constants.rnInvoiceCnt) - 1; orderPtr <= loopTo; orderPtr++) {
                    if (cp.Doc.GetBoolean(Constants.rnPaymentInvoice + orderPtr)) {
                        request.orderList.Add(DbBaseModel.create<OrderModel>(cp, cp.Doc.GetInteger(Constants.rnPaymentInvoice + orderPtr + "Id")));
                    }
                }
                //
                // set paymentAmount
                //
                request.paymentAmount = 0d;
                request.checkNumber = "";
                request.paymentMemo = "";
                request.creditCardOnFileId = 0;
                request.paymentCreditCard = new OnDemandMethodStruct();
                switch (request.paymentMethod) {
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.Cash: {
                            //
                            // -- cash
                            request.paymentAmount = cp.Doc.GetNumber(Constants.rnPaymentAmountCash);
                            request.paymentMemo = cp.Doc.GetText(Constants.rnPaymentMemo);
                            break;
                        }
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.Check: {
                            //
                            // -- check
                            request.paymentAmount = cp.Doc.GetNumber(Constants.rnPaymentAmountCheck);
                            request.checkNumber = cp.Doc.GetText(Constants.rncheckNumber);
                            break;
                        }
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.CreditCard: {
                            //
                            // -- credit card
                            request.paymentAmount = cp.Doc.GetNumber(Constants.rnPaymentAmountCreditCard);
                            request.paymentCreditCard = new OnDemandMethodStruct() {
                                creditCardNumber = cp.Doc.GetText(Constants.rnCreditCardNumber),
                                creditCardExpiration = (cp.Doc.GetInteger(Constants.rnCreditCardMonth) < 1 ? DateTime.MinValue : new DateTime(DateTime.Now.Year - 1 + cp.Doc.GetInteger(Constants.rnCreditCardYear), cp.Doc.GetInteger(Constants.rnCreditCardMonth), 1).AddMonths(1).AddDays(-1)).ToShortDateString(),
                                FirstName = GenericController.getFirstName(cp.Doc.GetText(Constants.rnCreditCardName)),
                                LastName = GenericController.getLastName(cp.Doc.GetText(Constants.rnCreditCardName)),
                                SecurityCode = cp.Doc.GetText(Constants.rnCreditCardCVV),
                                useAch = false,
                                Zip = cp.Doc.GetText(Constants.rnCreditCardZip)
                            };
                            break;
                        }
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.CreditCardStripeCheckout:
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.CreditCardOnFile: {
                            //
                            // -- credit card on file
                            request.creditCardOnFileId = cp.Doc.GetInteger("ondemandmethodid");
                            request.paymentAmount = cp.Doc.GetNumber(Constants.rnPaymentAmountCreditCard);
                            break;
                        }
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.Deposit: {
                            break;
                        }
                        //
                        // -- nothing to collect
                }
                //
                foreach (AccountAutoPayOptionModel giftCard in AccountAutoPayOptionModel.getGiftCardList(cp, request.accountId)) {
                    if (cp.Doc.GetBoolean(Constants.rnPaymentGiftCard + giftCard.id)) {
                        request.giftcardIdList.Add(giftCard.id);
                    }
                }
                //
                // -- the returnURL for stripe checkout
                request.stripeReturnUrl = $"https://{cp.Request.Host}{cp.Request.PathPage}?{cp.Doc.RefreshQueryString}";
                //
                // unappied deposts
                request.unappliedDepositIdList = new List<int>();
                using (var cs = cp.CSNew()) {
                    if (cs.Open("account transactions", ProcessingController.getSqlCriteriaUnappliedPayments(request.accountId))) {
                        do {
                            string unappliedDepositRequestName = Constants.rnPaymentDeposit + cs.GetInteger("id").ToString();
                            if (cp.Doc.GetBoolean(unappliedDepositRequestName)) {
                                request.unappliedDepositIdList.Add(cs.GetInteger("id"));
                            }
                            cs.GoNext();
                        }
                        while (cs.OK());
                    }
                    cs.Close();
                }
                string userError = "";
                if (!WorkflowController.workflow_processOrderPayment(app, ref userError, request)) {
                    cp.UserError.Add(userError);
                }
                return Constants.formIdToolPayment;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        // =================================================================================
        /// <summary>
        /// get Payment Form
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        internal string getForm(ApplicationModel app) {
            try {
                var cp = app.cp;
                //
                var layoutBuilder = app.cp.AdminUI.CreateLayoutBuilderNameValue();
                layoutBuilder.title = "Create Payment";
                layoutBuilder.description = "";
                layoutBuilder.includeForm = true;
                //
                string frameRqs = cp.Doc.GetText(Constants.rnFrameRqs);
                bool siteproperty_SortNewestToOldest = true;
                var nextPeriodStartDate = EcommerceExportModel.getNextPeriodStartDate(cp);
                string transactionDateLimitMessage;
                if (nextPeriodStartDate < DateTime.Parse("1900-01-01")) {
                    transactionDateLimitMessage = "";
                } else if (nextPeriodStartDate.ToLongTimeString() == "12:00:00 AM") {
                    transactionDateLimitMessage = $" (leave blank, or enter date on or after {nextPeriodStartDate.ToShortDateString()})";
                } else {
                    transactionDateLimitMessage = $" (leave blank, or enter date on or after {nextPeriodStartDate.ToLongTimeString()})";
                }
                string amountText = cp.Doc.GetText(Constants.rnPaymentAmountCreditCard);
                //
                // -- an order in a url from a different section. save it until payment method selected
                int pickOrderId = cp.Doc.GetInteger(Constants.rnPayFormPickOrderId);
                int AccountId = cp.Doc.GetInteger(Constants.rnAccountId);
                int pickAccountId = cp.Doc.GetInteger(Constants.rnPayFormPickAccountId);
                if (pickAccountId != 0) {
                    AccountId = pickAccountId;
                }
                if (pickOrderId > 0) {
                    var pickOrder = DbBaseModel.create<OrderModel>(cp, pickOrderId);
                    if (pickOrder is not null)
                        AccountId = pickOrder.accountId;
                }
                //
                if (string.IsNullOrEmpty(amountText) & pickOrderId != 0) {
                    //
                    // if amount not from previous form, and pickorderid<>0, then set this up to accept an amount at the end of the form
                    //
                    amountText = "{{pickOrderAmount}}";
                } else if (!string.IsNullOrEmpty(amountText) & !string.IsNullOrEmpty(amountText)) {
                    //
                    // if amounttext is a number, fix type
                    //
                    amountText = cp.Utils.EncodeNumber(amountText).ToString();
                } else if (pickOrderId != 0) {
                }
                //
                int payMethodId = cp.Doc.GetInteger(Constants.rnPayMethodId);
                int pickPayMethodId = cp.Doc.GetInteger(Constants.rnPayFormPickPayMethodId);
                if (pickPayMethodId != 0) {
                    payMethodId = pickPayMethodId;
                }
                //
                if (cp.Doc.GetBoolean("paymentApproved")) {
                    string qs = frameRqs;
                    AccountId = cp.Doc.GetInteger(Constants.rnAccountId);
                    qs = cp.Utils.ModifyQueryString(qs, Constants.rnDstFeatureGuid, "{E4B11C95-31E0-412E-94BE-6989ED09097C}");
                    qs = cp.Utils.ModifyQueryString(qs, "accountId", AccountId.ToString());
                    layoutBuilder.successMessage = $"Payment successful for account <a href=\"?{qs}\">{cp.Content.GetRecordName(Constants.cnAccounts, AccountId)}</a>";
                    //
                    // and start the selection over again
                    //
                    AccountId = 0;
                }
                layoutBuilder.addFormButton(Constants.buttonCancel);
                layoutBuilder.addFormHidden(Constants.rnSrcFormId, Constants.formIdToolPayment.ToString());
                double pickOrderAmount = 0d;
                //
                if (AccountId == 0) {
                    //
                    // form: select the account
                    //
                    layoutBuilder.addRow();
                    layoutBuilder.rowName = "Select Account";
                    layoutBuilder.rowValue = HtmlController.width400px(cp.Html.SelectContent(Constants.rnPayFormPickAccountId, AccountId.ToString(), Constants.cnAccounts, "((closed=0)or(closed is null))", "Select Account", "form-control", "abPaymentAccountSelect"));
                    //
                    string result = layoutBuilder.getHtml();
                    result = result.Replace("<form ", "<form id=\"abFormApplyPayment\" ");
                    result = result.Replace("{{pickOrderAmount}}", Strings.FormatNumber(pickOrderAmount, 2));
                    return result;
                }
                //
                // -- account selected
                layoutBuilder.addRow();
                layoutBuilder.rowName = "Account";
                layoutBuilder.rowValue = cp.Content.GetRecordName(Constants.cnAccounts, AccountId);
                layoutBuilder.addFormHidden(Constants.rnAccountId, AccountId.ToString());
                //
                bool hasDepositsAvailable = false;
                using (var cso = cp.CSNew()) {
                    hasDepositsAvailable = cso.Open("account transactions", ProcessingController.getSqlCriteriaUnappliedPayments(AccountId));
                    cso.Close();
                }
                bool isNoPaymentProcessor = !app.siteProperties.paymentProcessMethod.Equals(SitePropertyController.PaymentProcessMethodEnum.NoProcessorAvailableForceBilling);
                bool isStripeCheckout = app.siteProperties.paymentProcessMethod.Equals(SitePropertyController.PaymentProcessMethodEnum.StripeCheckout);
                bool isAllowCreditCardEntry = !isNoPaymentProcessor & !isStripeCheckout;
                //
                if (payMethodId == 0) {
                    //
                    // form: select the paymethod
                    //
                    layoutBuilder.addRow();
                    layoutBuilder.rowName = "Source of Funds";
                    layoutBuilder.rowValue = "";
                    //
                    // -- cash and check
                    layoutBuilder.rowValue += "" +
                        HtmlController.wrapFormCheck(HtmlController.getInputRadio(cp, Constants.rnPayFormPickPayMethodId, "4", payMethodId.ToString(), "abPayFormMethod form-check-input", "js-pay-cash") +
                        "<label class=\"form-check-label\" for=\"js-pay-cash\">Cash</label>") +
                        "" +
                        HtmlController.wrapFormCheck(HtmlController.getInputRadio(cp, Constants.rnPayFormPickPayMethodId, "1", payMethodId.ToString(), "abPayFormMethod form-check-input", "js-pay-check") +
                        "<label class=\"form-check-label\" for=\"js-pay-check\">Check</label>");
                    //
                    // -- card entry - processors that allow card in the site
                    if (isAllowCreditCardEntry) {
                        layoutBuilder.rowValue += "" +
                            HtmlController.wrapFormCheck(HtmlController.getInputRadio(cp, Constants.rnPayFormPickPayMethodId, "2", payMethodId.ToString(), "abPayFormMethod form-check-input", "js-pay-card") +
                            "<label class=\"form-check-label\" for=\"js-pay-card\">Credit Card Form</label>");
                    }
                    //
                    // -- stripe checkout
                    if (isStripeCheckout) {
                        layoutBuilder.rowValue += "" +
                            HtmlController.wrapFormCheck(HtmlController.getInputRadio(cp, Constants.rnPayFormPickPayMethodId, "6", payMethodId.ToString(), "abPayFormMethod form-check-input", "js-pay-card") +
                            "<label class=\"form-check-label\" for=\"js-pay-card\">Credit Card Stripe Checkout</label>");
                    }
                    //
                    // -- cards saved
                    bool hasCreditCardsOnFile = !AccountAutoPayOptionModel.getCreditCardCount(cp, AccountId).Equals(0);
                    layoutBuilder.rowValue += "" +
                        (hasCreditCardsOnFile ? HtmlController.wrapFormCheck(HtmlController.getInputRadio(cp, Constants.rnPayFormPickPayMethodId, "5", payMethodId.ToString(), "abPayFormMethod form-check-input", "js-pay-account") +
                        "<label class=\"form-check-label\" for=\"js-pay-account\">Credit Card On File</label>") : "");
                    //
                    // -- deposits
                    bool hasGiftCards = AccountAutoPayOptionModel.getGiftCardList(cp, AccountId).Count>0;
                    layoutBuilder.rowValue += "" +
                        (hasDepositsAvailable || hasGiftCards ? HtmlController.wrapFormCheck(HtmlController.getInputRadio(cp, Constants.rnPayFormPickPayMethodId, "3", payMethodId.ToString(), "abPayFormMethod form-check-input", "js-pay-deposit") + "<label class=\"form-check-label\" for=\"js-pay-deposit\">Deposits, Gift Cards</label>") : "") +
                        "";
                    layoutBuilder.addFormHidden(Constants.rnPayFormPickOrderId, pickOrderId.ToString());
                    //
                    string result = layoutBuilder.getHtml();
                    result = result.Replace("<form ", "<form id=\"abFormApplyPayment\" ");
                    result = result.Replace("{{pickOrderAmount}}", Strings.FormatNumber(pickOrderAmount, 2));
                    return result;
                }
                //
                // -- payMethod selected
                //
                layoutBuilder.addFormHidden(Constants.rnPayMethodId, payMethodId.ToString());
                //
                switch (payMethodId) {
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.CreditCardStripeCheckout: {
                            //
                            // credit card stripe
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Payment Method";
                            layoutBuilder.rowValue = "Credit Card Stripe Checkout";
                            break;
                        }
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.CreditCard: {
                            //
                            // credit card
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Payment Method";
                            layoutBuilder.rowValue = "Credit Card Form";
                            break;
                        }
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.CreditCardOnFile: {
                            //
                            // credit card
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Payment Method";
                            layoutBuilder.rowValue = "Card On File";
                            break;
                        }
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.Cash: {
                            //
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Payment Method";
                            layoutBuilder.rowValue = "Cash";
                            break;
                        }
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.Check: {
                            //
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Payment Method";
                            layoutBuilder.rowValue = "Check";
                            break;
                        }

                    default: {
                            //
                            // apply previous unapplied payments
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Payment Method";
                            layoutBuilder.rowValue = "Apply Deposit";
                            break;
                        }
                }
                //
                // -- Unpaid Invoices row,  List Invoices with non-zero balances, A zero balance is determined by totally all the transactions set to this invoice
                string Link = cp.AdminUI.GetPortalFeatureLink(Constants.guidPortalAccountManager,Constants.guidPortalFeaturePay);
                Link = cp.Utils.ModifyQueryString(Link, Constants.rnAccountId, "0", true);
                string sql = $"(accountId={AccountId})and(dateCompleted is not null)and(dateCanceled is null)and(payDate is null)";




                // 20210723 removed due to credit invoices not showing in the payment tool
                // & "and(totalCharge>=0)" _

                string sortFieldList = "dateDue";
                if (siteproperty_SortNewestToOldest) {
                    sortFieldList = "dateDue Desc";
                }
                bool invoiceSelected;
                layoutBuilder.addRow();
                layoutBuilder.rowName = "Invoices To Pay";
                string rowValue = "";
                //
                using (var cs = cp.CSNew()) {
                    int invoicePtr = 0;
                    bool invoicesFound = false;
                    cs.Open("orders", sql, sortFieldList, true, "", 99999, 1);
                    while (cs.OK()) {
                        int orderId = cs.GetInteger("id");
                        double orderAmount = cs.GetNumber("totalCharge");
                        invoiceSelected = false;
                        if (orderId == pickOrderId) {
                            invoiceSelected = true;
                            pickOrderAmount += orderAmount;
                        } else if (cp.Doc.GetBoolean(Constants.rnPaymentInvoice + invoicePtr)) {
                            invoiceSelected = true;
                            pickOrderAmount += orderAmount;
                        }
                        invoicesFound = true;
                        string lineItemHtml;
                        if (orderAmount < 0d) {
                            string label = $"<label class=\"m-0 p-0\" for=\"{Constants.rnPaymentInvoice}{invoicePtr}\">&nbsp;Credit #{orderId},&nbsp;{cs.GetText("name")},&nbsp;Created:{GenericController.encodeShortDateString(cs.GetDate("dateAdded"))},&nbsp;Balance:{Strings.FormatCurrency(orderAmount, 2)}</label>";




                            lineItemHtml = cp.Html.CheckBox(Constants.rnPaymentInvoice + invoicePtr, invoiceSelected, "afwPaymentInvoice", Constants.rnPaymentInvoice + invoicePtr) + label;
                        } else {
                            string label = $"<label class=\"m-0 p-0\" for=\"{Constants.rnPaymentInvoice}{invoicePtr}\">&nbsp;Invoice {orderId},&nbsp;{cs.GetText("name")},&nbsp;Created:{GenericController.encodeShortDateString(cs.GetDate("dateAdded"))},&nbsp;Due:{GenericController.encodeShortDateString(cs.GetDate("dateDue"))},&nbsp;Balance:{Strings.FormatCurrency(orderAmount, 2)}</label>";






                            lineItemHtml = cp.Html.CheckBox(Constants.rnPaymentInvoice + invoicePtr, invoiceSelected, "afwPaymentInvoice", Constants.rnPaymentInvoice + invoicePtr) + label;

                        }
                        rowValue += cp.Html5.Div(lineItemHtml);
                        layoutBuilder.addFormHidden($"{Constants.rnPaymentInvoice}{invoicePtr}Id", orderId.ToString());
                        layoutBuilder.addFormHidden($"{Constants.rnPaymentInvoice}{invoicePtr}Amount", orderAmount.ToString());
                        invoicePtr += 1;
                        cs.GoNext();
                    }
                    if (!invoicesFound) {
                        rowValue = "This account has no unpaid invoices";
                    }
                    layoutBuilder.rowValue = rowValue;
                    layoutBuilder.addFormHidden(Constants.rnInvoiceCnt, invoicePtr.ToString());
                    cs.Close();
                }
                //
                // -- Unpaid Deposits row
                if (hasDepositsAvailable) {
                    string orderBy = "dateAdded";
                    if (siteproperty_SortNewestToOldest) {
                        orderBy += " desc";
                    }
                    using (var cs = cp.CSNew()) {
                        string sqlCriteriaUnappliedPayments = ProcessingController.getSqlCriteriaUnappliedPayments(AccountId);
                        if (cs.Open("account transactions", sqlCriteriaUnappliedPayments, orderBy)) {
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Deposits To Apply";
                            int depositPtr = 0;
                            do {
                                int depositId = cs.GetInteger("id");
                                double depositAmount = -cs.GetNumber("amount");
                                string requestName = Constants.rnPaymentDeposit + depositId.ToString();
                                string depositName = !string.IsNullOrEmpty(cs.GetText("name")) ? cs.GetText("name") : $"deposit {depositId}";
                                string label = $"<label class=\"m-0 p-0\" for=\"{Constants.rnPaymentDeposit}{depositPtr}\">&nbsp{Strings.FormatCurrency(depositAmount, 2)}, {depositName}, deposited {Conversions.ToString(cs.GetDate("dateAdded"))}</label>";
                                string depositHtml = cp.Html.CheckBox(requestName, cp.Doc.GetBoolean(requestName), Constants.rnPaymentDeposit, Constants.rnPaymentDeposit + depositPtr) + label;
                                layoutBuilder.rowValue += cp.Html5.Div(depositHtml);
                                layoutBuilder.addFormHidden($"{Constants.rnPaymentDeposit}{depositPtr}Id", depositId.ToString());
                                layoutBuilder.addFormHidden($"{Constants.rnPaymentDeposit}{depositPtr}Amount", depositAmount.ToString());
                                depositPtr += 1;
                                cs.GoNext();
                            }
                            while (cs.OK());
                        }
                        cs.Close();
                    }
                }
                //
                // -- Gift Cards
                var giftCardList = AccountAutoPayOptionModel.getGiftCardList(cp, AccountId);
                if (giftCardList.Count > 0) {
                    layoutBuilder.addRow();
                    layoutBuilder.rowName = "Gift Cards To Apply";
                    int giftCardPtr = 0;
                    foreach (AccountAutoPayOptionModel giftCard in giftCardList) {
                        string giftCardName = !string.IsNullOrEmpty(giftCard.name) ? giftCard.name : $"Gift Card {giftCard.id}";
                        double giftCardAmount = giftCard.giftCardOriginalAmount - giftCard.giftCardAmountSpent;
                        string htmlName = Constants.rnPaymentGiftCard + giftCard.id;
                        string htmlId = Constants.rnPaymentGiftCard + giftCardPtr.ToString();
                        string htmlClass = Constants.rnPaymentGiftCard;
                        string label = $"<label class=\"m-0 p-0\" for=\"{htmlId}\">&nbsp;{giftCardName}, amount: {Strings.FormatCurrency(giftCardAmount, 2)}</label for=\"{htmlName}\">";
                        layoutBuilder.rowValue += cp.Html5.Div(cp.Html5.CheckBox(htmlName, cp.Doc.GetBoolean(htmlName), htmlClass, htmlId) + label);
                        layoutBuilder.addFormHidden($"{Constants.rnPaymentGiftCard}{giftCardPtr}Id", giftCard.id.ToString());
                        layoutBuilder.addFormHidden($"{Constants.rnPaymentGiftCard}{giftCardPtr}Amount", giftCardAmount.ToString());
                        giftCardPtr += 1;
                    }
                }
                //
                switch (payMethodId) {
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.CreditCard: {
                            //
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Credit Card Number";
                            layoutBuilder.rowValue = HtmlController.width400px(cp.Html5.InputText(Constants.rnCreditCardNumber, 255, cp.Doc.GetText(Constants.rnCreditCardNumber), "form-control", "js-pay-number"));
                            string creditCardYearList = "";
                            for (int ptr = 0; ptr <= 10; ptr++)
                                creditCardYearList += $",{DateTime.Now.AddYears(ptr).Year}";
                            creditCardYearList = creditCardYearList.Substring(1);
                            //
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Card Expiration";
                            int creditCardMonth = cp.Doc.GetInteger(Constants.rnCreditCardMonth);
                            int creditCardYear = cp.Doc.GetInteger(Constants.rnCreditCardYear);
                            layoutBuilder.rowValue = $"{HtmlController.width100px(cp.Html5.SelectList(Constants.rnCreditCardMonth, creditCardMonth.ToString(), "01-Jan,02-Feb,03-Mar,04-Apr,05-May,06-June,07-July,08-Aug,09-Sep,10-Oct,11-Nov,12-Dec", "Month", "form-control", "js-pay-card-month"))}&nbsp;/&nbsp;{HtmlController.width100px(cp.Html5.SelectList(Constants.rnCreditCardYear, creditCardYear.ToString(), creditCardYearList, "Year", "form-control", "js-pay-card-year"))}";



                            //
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Name on Card";
                            layoutBuilder.rowValue = HtmlController.width400px(cp.Html5.InputText(Constants.rnCreditCardName, 255, cp.Doc.GetText(Constants.rnCreditCardName), "form-control", "js-pay-card-name"));
                            //
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Card Security Code";
                            layoutBuilder.rowValue = HtmlController.width400px(cp.Html5.InputText(Constants.rnCreditCardCVV, 255, cp.Doc.GetText(Constants.rnCreditCardCVV), "form-control", "js-pay-card-code")) + HtmlController.wrapFieldHelp("(optional) 3-digit code found on the back of the card. if should never be saved so it signals the card is in-hand.");
                            //
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Card Billing Zipcode";
                            layoutBuilder.rowValue = HtmlController.width400px(cp.Html5.InputText(Constants.rnCreditCardZip, 255, cp.Doc.GetText(Constants.rnCreditCardZip), "form-control", "js-pay-card-zip")) + HtmlController.wrapFieldHelp("(optional) The zipcode of the credit card billing address. The payment processor charge may be lower if this is provided.");
                            break;
                        }
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.CreditCardOnFile: {
                            //
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Credit Card";
                            var cardList = AccountAutoPayOptionModel.getCreditCardACHList(cp, AccountId);
                            if (cardList.Count > 0) {
                                foreach (AccountAutoPayOptionModel card in cardList) {
                                    CreditCardController.validatePayOptionEncryption(app, card);
                                    //
                                    // -- get card last-four
                                    string CardLabel = card.ccNumberEncryptedLastFour;
                                    if (!app.siteProperties.creditCardEncryption) {
                                        CardLabel = CreditCardController.getLastFour(card.ccNumber);
                                    }
                                    //
                                    if (cardList.Count == 1) {
                                        layoutBuilder.rowValue += cp.Html5.Div(cp.Html5.Hidden("ondemandmethodid", card.id) + $"&nbsp;{CardLabel}, expiration: {card.ccExpiration.ToString("y")}");
                                    } else {
                                        layoutBuilder.rowValue += cp.Html5.Div(cp.Html5.RadioBox("ondemandmethodid", card.id, cp.Doc.GetInteger("ondemandmethodid")) + $"&nbsp;{CardLabel}, expiration: {card.ccExpiration.ToString("y")}");
                                    }
                                }
                            }

                            break;
                        }
                    //
                    // form.closeFieldSet()
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.Cash: {
                            //
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Memo";
                            layoutBuilder.rowValue = HtmlController.width400px(cp.Html5.InputText(Constants.rnPaymentMemo, 4000, cp.Doc.GetText(Constants.rnPaymentMemo), "form-control"));
                            layoutBuilder.rowHelp = "Optional. This message will be added to the payment transaction and can be seen in the account history. It is not visible to the customer";
                            break;
                        }
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.Check: {
                            //
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Check Number";
                            layoutBuilder.rowValue = HtmlController.width400px(cp.Html5.InputText(Constants.rncheckNumber, 255, cp.Doc.GetText(Constants.rncheckNumber), "form-control", "js-pay-check-number"));
                            break;
                        }

                    default: {
                            break;
                        }
                        //
                        // no else case
                }
                //
                layoutBuilder.addFormButton(Constants.buttonProcessPayment, Constants.rnButton, "abPaymentOK");
                //
                // -- Amount row for outside payment
                //
                switch (payMethodId) {
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.Cash: {
                            //
                            // cash
                            //
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Cash Amount";
                            string cashAmountText = cp.Doc.GetNumber(Constants.rnPaymentAmountCash).ToString("0.00");
                            cashAmountText = cashAmountText.Equals("0.00") ? "" : cashAmountText;
                            string htmlInput = $"<input type=\"number\" step=\"0.01\" name=\"{Constants.rnPaymentAmountCash}\" id=\"js-pay-cash-amount\" class=\"afwPaymentAmount form-control\" maxlength=\"255\" size=\"20\" value=\"{cashAmountText}\">";
                            layoutBuilder.rowValue = HtmlController.width400px(htmlInput);
                            break;
                        }
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.Check: {
                            //
                            // check
                            //
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Check Amount";
                            string checkAmountText = cp.Doc.GetNumber(Constants.rnPaymentAmountCheck).ToString("0.00");
                            checkAmountText = checkAmountText.Equals("0.00") ? "" : checkAmountText;
                            string htmlInput = $"<input type=\"number\" step=\"0.01\" name=\"{Constants.rnPaymentAmountCheck}\" id=\"js-pay-check-amount\" class=\"afwPaymentAmount form-control\" maxlength=\"255\" size=\"20\" value=\"{checkAmountText}\">";
                            layoutBuilder.rowValue = HtmlController.width400px(htmlInput);
                            break;
                        }
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.CreditCardOnFile:
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.CreditCardStripeCheckout:
                    case (int)ProcessPaymentApiRequest.PaymentMethodEnum.CreditCard: {
                            //
                            // credit card
                            //
                            layoutBuilder.addRow();
                            layoutBuilder.rowName = "Card Amount";
                            string cardAmountText = cp.Doc.GetNumber(Constants.rnPaymentAmountCreditCard).ToString("0.00");
                            cardAmountText = cardAmountText.Equals("0.00") ? "" : cardAmountText;
                            string htmlInput = $"<input type=\"number\" step=\"0.01\" name=\"{Constants.rnPaymentAmountCreditCard}\" id=\"js-pay-card-amount\" class=\"afwPaymentAmount form-control\" maxlength=\"255\" size=\"20\" value=\"{cardAmountText}\">";
                            layoutBuilder.rowValue = HtmlController.width400px(htmlInput);
                            break;
                        }
                    default: {
                            break;
                        }
                }
                //
                // -- create a deposit row
                layoutBuilder.addRow();
                layoutBuilder.rowName = "Create Deposit";
                string createDepositAmountText = cp.Doc.GetNumber(Constants.rnPaymentAmountCreateDeposit).ToString("0.00");
                createDepositAmountText = createDepositAmountText.Equals("0.00") ? "" : createDepositAmountText;
                string createDepositAmountInput = $"<input type=\"number\" step=\"0.01\" name=\"{Constants.rnPaymentAmountCreateDeposit}\" id=\"js-pay-create-deposit-amount\" class=\"form-control\" maxlength=\"255\" size=\"20\" value=\"{createDepositAmountText}\" readonly>";
                layoutBuilder.rowValue = $"{HtmlController.wrapFormCheck(cp.Html5.CheckBox(Constants.rnPaymentCreatedeposit, cp.Doc.GetBoolean(Constants.rnPaymentCreatedeposit), "form-check-input", "abPaymentCreateDeposit") + HtmlController.wrapFieldHelp("Check to deposit this amount. The deposit amount is the total of payments and deposits less the amount of invoices to pay. Gift cards are only applied as needed and cannot have excess to deposit."))}{HtmlController.width400px(createDepositAmountInput, "mt-2")}";

                //
                layoutBuilder.addRow();
                layoutBuilder.rowName = "No Receipt";
                layoutBuilder.rowValue = HtmlController.wrapFormCheck(cp.Html5.CheckBox(Constants.rnPaymentBlockReceipt, cp.Doc.GetBoolean(Constants.rnPaymentBlockReceipt), "form-check-input", "js-pay-block-receipt") + HtmlController.wrapFieldHelp("If checked, no receipt will be sent to the customer after the payment."));
                //
                layoutBuilder.addRow();
                layoutBuilder.rowName = "Transaction Date";
                var paymentTransactionDatePrepopulate = cp.Doc.GetDate(Constants.rnPaymentTransactionDate);
                if (paymentTransactionDatePrepopulate < DateTime.Parse("1900-01-01")) {
                    paymentTransactionDatePrepopulate = app.rightNow.Date;
                }
                layoutBuilder.rowValue = HtmlController.width400px(cp.Html5.InputDate(Constants.rnPaymentTransactionDate, paymentTransactionDatePrepopulate, "form-control", "js-pay-card-transaction-date") + transactionDateLimitMessage);
                //
                string resultFinal = layoutBuilder.getHtml();
                resultFinal = resultFinal.Replace("<form ", "<form id=\"abFormApplyPayment\" ");
                resultFinal = resultFinal.Replace("{{pickOrderAmount}}", Strings.FormatNumber(pickOrderAmount, 2));
                return resultFinal;
            } catch (Exception ex) {
                app.cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
