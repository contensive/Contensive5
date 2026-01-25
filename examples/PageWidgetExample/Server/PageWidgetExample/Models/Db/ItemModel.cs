using Contensive.Models.Db;

namespace Contensive.PageWidgetExample.Models.Db {
    public class ItemModel : DbBaseModel {
        // 
        // ====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; private set; } = new DbBaseTableMetadataModel("items", "items", "default", false);
        // 
        // ====================================================================================================
        /// <summary>
        /// The primary image for the item. It is used for SEO and the catalog list page
        /// </summary>
        /// <returns></returns>
        public FieldTypeFile imageFilename { get; set; }
        /// <summary>
        /// the alternate sizes created for the imageFilename, saved as a comma delimited string "filename,widthxheight,widthxheight,etc"
        /// </summary>
        /// <returns></returns>
        public string altSizeList { get; set; }
        /// <summary>
        /// short copy used on the catalog listing page
        /// </summary>
        /// <returns></returns>
        public string overview { get; set; }



        // ''' <summary>
        // ''' ignored, research cart to restore
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property affiliatePayment As Double
        // ''' <summary>
        // ''' ignored, research cart to restore
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property affiliateSetupPayment As Double
        // ''' <summary>
        // ''' default: false, when true item's invoiceCopy is added to the invoices message
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property allowInvoiceCopy As Boolean
        // ''' <summary>
        // ''' default: false, when true the items receiptCopy is added to paid invoices
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property allowReceiptCopy As Boolean
        // '''' <summary>
        // '''' ignored, research card to restore
        // '''' </summary>
        // '''' <returns></returns>
        // 'Public Property altThumbSizeList As String
        // ''' <summary>
        // ''' default: false, if true, there can only be one recurring purchase (subscription) on an account from this item
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property blockDupRecurringPurchases As Boolean
        // ''' <summary>
        // ''' deprecated. Use overview instead
        // ''' </summary>
        // ''' <returns></returns>
        // <Obsolete("deprecated, use overview instead", True)>
        // Public Property briefFilename As DbBaseModel.FieldTypeHTMLFile
        // ''' <summary>
        // ''' category, like Breakfast, Luch, Diner, Side. Ecommerce catalogs can use this to categorize, filter or display just one category
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property categoryID As Integer
        // ''' <summary>
        // ''' The main copy for the catalog item detail page
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property copy As String
        // ''' <summary>
        // ''' deprecated, use copy
        // ''' </summary>
        // ''' <returns></returns>
        // ''' 
        // <Obsolete("deprecated, use copy instead", True)>
        // Public Property copyFilename As DbBaseModel.FieldTypeHTMLFile
        // ''' <summary>
        // ''' legacy, after a purchase the accounting for the item should take place on this date (I think)
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property defermentDate As Date?
        // ''' <summary>
        // ''' The GL category to be used to hold the purchased item until the deferment date (I think)
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property defermentGeneralLedgerAccountId As Integer
        // ''' <summary>
        // ''' A flag to use for catelog filtering, or for custom item lists (future)
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property featured As Boolean
        // ''' <summary>
        // ''' This group gets a notification when the fulfillment stage occurs
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property fulfillmentNotificationGroupId As Integer
        // ''' <summary>
        // ''' the GL account where the purchase is posted
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property generalLedgerAccountId As Integer
        // ''' <summary>
        // ''' legacy
        // ''' </summary>
        // ''' <returns></returns>
        // <Obsolete("deprecated", True)> Public Property generalLedgerPostMethodId As Integer
        // ''' <summary>
        // ''' If this item is a gift card and this property is set, catalog options will be verified, and on purchase, this email will be sent to this user
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property giftCardRecipientEmailid As Integer
        // ''' <summary>
        // ''' id mwmbership expiration is periodic, the period is determined by adding groupExpirationPeriod (days) and groupExpirationPeriodMonths (months)
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property groupExpirationPeriod As Integer
        // ''' <summary>
        // ''' if membership expiration is periodic, the period is determined by adding groupExpirationPeriod (days) and groupExpirationPeriodMonths (months)
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property groupExpirationPeriodMonths As Integer
        // ''' <summary>
        // ''' Subscription group. All users in the account are added to this group. All non-subscribed users are removed
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property groupID As Integer
        // ''' <summary>
        // ''' If this item is free, block the invoice line item
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property hideInvoiceLineIfNoCharge As Boolean
        // ''' <summary>
        // ''' The primary image for the item. It is used for SEO and the catalog list page
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property inMyAccount As Boolean
        // ''' <summary>
        // ''' This copy is added to the invoice message
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property invoiceCopy As String
        // ''' <summary>
        // ''' if true, the commission account gets a commission for all purchases of this item
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property isCommissionable As Boolean
        // ''' <summary>
        // ''' This item is a gift card. When purchased, the customer receives an email to redeem the gift card.
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property isGiftCard As Boolean
        // ''' <summary>
        // ''' if true, this item displays in the catalog
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property isInCatalog As Boolean
        // ''' <summary>
        // ''' if true and this item is a subscription (has a subscruption type set), it will be automatically repurchased when it expires
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property isRecurringPurchase As Boolean
        // ''' <summary>
        // ''' if true, and the shipping address has a state record with a tax rate, that tax rate will be added to the order
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property isTaxable As Boolean
        // ''' <summary>
        // ''' Use for internal identification
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property itemNumber As String
        // ''' <summary>
        // ''' default: none=0, Calendar=1, Periodic=2, Perpetual=3, see enum ItemModel.SubscriptionTypes
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property membershipDurationTypeId As Integer
        // '''' <summary>
        // '''' The number of days a subscription stays active after the invoice due date.
        // '''' </summary>
        // '''' <returns></returns>
        // 'Public Property membershipGracePeriodDays As Integer
        // ''' <summary>
        // ''' if subscription type is calendar, this is the month it expires
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property membershipMonthId As Integer
        // ''' <summary>
        // ''' A messase is sent to the billing contact when invoices with the item are paid
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property msgToBillingWhenPaid As String
        // ''' <summary>
        // ''' (legacy) the number of deferment transactions created
        // ''' </summary>
        // ''' <returns></returns>
        // <Obsolete("deprecated", True)> Public Property numberOfDefermentTransactions As Integer
        // ''' <summary>
        // ''' if true, the sale price is used
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property onSale As Boolean
        // ''' <summary>
        // ''' A list of lines with option-name=option-value2,option-value1,option-value3, etc.
        // ''' an item with options will require all op[tions to be completed to be ordered
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property options As String
        // ''' <summary>
        // ''' Order button can be on, off or only available if you are a member.
        // ''' 1 = Always Hide
        // ''' 2 = Always Show
        // ''' 3 = Show Based on User Group Policies
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property orderButtonModeId As Integer
        // ''' <summary>
        // ''' manufacturer (made by)
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property organizationID As Integer
        // ''' <summary>
        // ''' item can be forced to require ondemand even for billing accounts, or allowed to be billed even for ondemand accounts.
        // ''' 1 = Any Pay Method
        // ''' 2 = On-Demand Required
        // ''' 3 = On-Demand If Available
        // ''' 4 = Billing Allowed
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property payMethodCompatibility As Integer
        // ''' <summary>
        // ''' When this item is purchased, this email will be sent
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property purchaseEmailId As Integer
        // ''' <summary>
        // ''' inventory low quantity alarm. If the q gets below this, an alert is sent
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property quantityLow As Integer
        // ''' <summary>
        // ''' When true, only one of this item can appear on an order (not even 
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property orderAlone As Boolean
        // ''' <summary>
        // ''' The max number that can be added to an order. These can appear with other items in the order, and multiple subscription periods only count as the one item.
        // ''' Zero means no limit.
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property quantityMax As Integer
        // ''' <summary>
        // ''' The max number that can be added to this account.
        // ''' Zero means no limit.
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property quantityMaxAccount As Integer
        // ''' <summary>
        // ''' Inventory - how many should be there
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property quantityOnHand As Integer
        // ''' <summary>
        // ''' This text is added to the recipts message areas
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property receiptCopy As String
        // ''' <summary>
        // ''' If not empty, this price is used during recurring purchses. If empty, the unit price is used
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property recurringPrice As Double?
        // ''' <summary>
        // ''' The number of times this subscription can be renewed. The subscription is canceled when the count clicks from 1 to 0
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property renewCount As Integer?
        // ''' <summary>
        // ''' when the item is renewed, send this email to the account primary contact
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property renewEmailId As Integer
        // ''' <summary>
        // ''' if the item is a subscription, when the item is renewed, renew with this item instead
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property renewItemid As Integer
        // ''' <summary>
        // ''' (legacy) log the GL activity at the time the item is purchased, not when it is paid
        // ''' </summary>
        // ''' <returns></returns>
        // <Obsolete("deprecated", True)> Public Property reportCashBasis As Boolean
        // ''' <summary>
        // ''' if not null, and onSale is true, this is teh sale price charged
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property salePrice As Double?
        // ''' <summary>
        // ''' if true, a fulfillment notification is sent during fulfillment
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property sendFulfillmentNotification As Boolean
        // ''' <summary>
        // ''' if true, a renewal notification is send before the item expires
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property sendRenewalNotification As Boolean
        // ''' <summary>
        // ''' Use allowShip, allowDeliver, allowPickup, allowInHouse
        // ''' </summary>
        // ''' <returns></returns>
        // <Obsolete("deprecated", False)> Public Property shippingRequired As Boolean
        // ''' <summary>
        // ''' If checked, this item is delivered digitally, not requiring any physical shipping or delivery.
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property allowDigitalDelivery As Boolean
        // ''' <summary>
        // ''' allow this item to be shipped by a third party shipper
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property allowShip As Boolean
        // ''' <summary>
        // ''' allow this item to be delivered by an employee
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property allowDelivery As Boolean
        // ''' <summary>
        // ''' allow this item to be prepackaged for pickup
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property allowPickup As Boolean
        // ''' <summary>
        // ''' allow this item to be shopped in-house (dine-in, in-stock, etc)
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property allowInHouse As Boolean
        // ''' <summary>
        // ''' if true, bundled items will be shown on invoices. If false, they aer hidden
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property showBundledItems As Boolean
        // ''' <summary>
        // ''' The manufacturers item number
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property sourceItemNumber As String
        // ''' <summary>
        // ''' the member price, applicable to the member group
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property specialPrice As Double?
        // ''' <summary>
        // ''' The unit cost, used for reporting
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property unitCost As Double
        // ''' <summary>
        // ''' The unit price
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property unitPrice As Double
        // ''' <summary>
        // ''' null if not entered. This is the suggested retail price
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property unitRetail As Double?
        // ''' <summary>
        // ''' The upc for the item
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property qrCodeData As String
        // ''' <summary>
        // ''' A 12 character string to be displayed as a UPS in the catalog view
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property upc As String
        // ''' <summary>
        // ''' number of times the detail page was viewed
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property viewings As Integer
        // ''' <summary>
        // ''' the shipping weight
        // ''' </summary>
        // ''' <returns></returns>
        // Public Property weight As Double
        // ''' <summary>
        // ''' deprecated
        // ''' </summary>
        // ''' <returns></returns>
        // <Obsolete("deprecated", True)> Public Property recurringPrebillDays As Integer
        // ''' <summary>
        // ''' The choices for membershipDurationType
        // ''' </summary>
        // Public Enum SubscriptionDurationTypeEnum
        // ''' <summary>
        // ''' 0 = when the subscription expires, it does not recur
        // ''' </summary>
        // None = 0
        // ''' <summary>
        // ''' recurs on a calendar day
        // ''' </summary>
        // Calendar = 1
        // ''' <summary>
        // ''' recurs after a specific period of time
        // ''' </summary>
        // Periodic = 2
        // ''' <summary>
        // ''' subscription never expires
        // ''' </summary>
        // Perpetual = 3
        // End Enum
        // '
        // '============================================================================================================================================================
        // ''' <summary>
        // ''' convert the itemOption domain model into a string to be stored in the record
        // ''' </summary>
        // ''' <param name="itemOptionList"></param>
        // ''' <returns></returns>
        // Public Shared Function serializeOptions(itemOptionList As List(Of Domain.ItemOptionModel)) As String
        // Try
        // Dim result As New StringBuilder()
        // For Each itemOption As Domain.ItemOptionModel In itemOptionList
        // result.Append(itemOption.name & ":")
        // Dim delimiter As String = ""
        // For Each optionChoice As ItemOptionChoiceModel In itemOption.choiceList
        // result.Append(delimiter & optionChoice.name)
        // If Not optionChoice.charge.Equals(0) Then result.Append("/" & optionChoice.charge.ToString("0.00"))
        // delimiter = ","
        // Next
        // Next
        // Return result.ToString()
        // Catch ex As Exception
        // Throw New GenericException("ItemModel.serializeOptions exception", ex)
        // End Try
        // End Function
        // '
        // '============================================================================================================================================================
        // ''' <summary>
        // ''' convert the option field string  (color:red/2.50,blue/1.25) into the itemOptionModel domain model
        // ''' </summary>
        // ''' <param name="itemOptions"></param>
        // ''' <returns></returns>
        // Public Shared Function deserializeOptions(itemOptions As String) As List(Of Domain.ItemOptionModel)
        // Try
        // Dim result As New List(Of Domain.ItemOptionModel)
        // If String.IsNullOrEmpty(itemOptions) Then Return result
        // '
        // Dim optionPtr As Integer = 0
        // Dim optionChoicePtr As Integer = 0
        // For Each optionRow As String In itemOptions.Split(ControlChars.CrLf.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
        // '
        // ' -- convert this "*!!name:value/2.34,value/5.67" into a list item
        // If Not String.IsNullOrWhiteSpace(optionRow) Then
        // '
        // ' -- (0)=*!!name
        // ' -- (1)=value/2.34,value/5.67
        // ' -- parse (0) into optionName, optionRequired and choiceCount
        // Dim optionNameValues As String() = optionRow.Split(":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
        // Dim optionName As String = optionNameValues(0).Trim()
        // Dim optionRequired As Boolean = False
        // Dim choiceCount As Integer = 0
        // For Each chr As Char In optionName
        // If (chr.Equals("*"c)) Then
        // '
        // ' -- option required
        // optionRequired = True
        // Continue For
        // End If
        // If (chr.Equals("!"c)) Then
        // '
        // ' -- allow/require one more pick
        // choiceCount += 1
        // Continue For
        // End If
        // Next
        // optionName = optionName.Replace("!", "").Replace("*", "")
        // '
        // ' -- parse choices
        // Dim optionChoices As New List(Of Domain.ItemOptionChoiceModel)
        // If (optionNameValues.Length.Equals(2)) Then
        // '
        // ' -- read list of values (list of value/charge)
        // For Each optionChoice As String In optionNameValues(1).Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
        // Dim valueCharge As String() = optionChoice.Replace("\", "/").Split("/"c)
        // Dim optionChoiceName As String = valueCharge(0).Trim()
        // Dim optionChoiceCharge As Double = 0
        // If valueCharge.Length.Equals(2) Then
        // '
        // ' -- read charge from value
        // Dim normalizedCharge As String = valueCharge(1).Replace("$", "")
        // If Not Double.TryParse(normalizedCharge, optionChoiceCharge) Then optionChoiceCharge = 0
        // End If
        // optionChoices.Add(New Domain.ItemOptionChoiceModel() With {
        // .name = optionChoiceName,
        // .id = optionChoicePtr,
        // .charge = optionChoiceCharge
        // })
        // optionChoicePtr += 1
        // Next
        // End If
        // '
        // ' -- special case - if not required and no choice cnt specified, you can choose up to all
        // If (Not optionRequired And choiceCount.Equals(0)) Then choiceCount = optionChoices.Count
        // '
        // ' add to result
        // result.Add(New Domain.ItemOptionModel() With {
        // .name = optionName,
        // .id = optionPtr,
        // .required = optionRequired,
        // .choicesToPick = choiceCount,
        // .choiceList = optionChoices
        // })
        // optionPtr += 1
        // End If
        // Next
        // Return result
        // Catch ex As Exception
        // Throw New GenericException("ItemModel.deserializeOptions exception", ex)
        // End Try
        // End Function
    }
}