# CreateLayoutBuilderNameValue Pattern Example

This folder contains a complete example demonstrating the `CreateLayoutBuilderNameValue` pattern used in Contensive AdminUI development.

## Overview

The `CreateLayoutBuilderNameValue` method creates a builder object for constructing form-based views with a name-value pair layout. This pattern is ideal for:

- Multi-step forms (wizards)
- Settings/configuration pages
- Detail/edit forms
- Payment/transaction forms

The pattern provides a structured way to create forms with:
- Name-value row pairs (label on left, input/content on right)
- Form submission handling
- Success/error messages
- Hidden fields and buttons
- Optional help text per row

## Example File

**AccountManagerToolPaymentAddon.cs** - Demonstrates a multi-step payment processing form using the LayoutBuilderNameValue pattern.

## Key Pattern Elements

### 1. Initialize the LayoutBuilder

```csharp
var layoutBuilder = app.cp.AdminUI.CreateLayoutBuilderNameValue();
layoutBuilder.title = "Create Payment";
layoutBuilder.description = "";
layoutBuilder.includeForm = true;
```

### 2. Add Rows with Name-Value Pairs

For each form row, set the name, value, and optional help text, then call `addRow()`:

```csharp
layoutBuilder.addRow();
layoutBuilder.rowName = "Account";
layoutBuilder.rowValue = cp.Content.GetRecordName(Constants.cnAccounts, AccountId);
layoutBuilder.rowHelp = "Optional help text"; // Optional
```

### 3. Success Messages

Display success feedback at the top of the form:

```csharp
layoutBuilder.successMessage = "Payment successful for account <a href=\"?qs\">Account Name</a>";
```

### 4. Add Form Elements

Add buttons and hidden fields:

```csharp
layoutBuilder.addFormButton(Constants.buttonCancel);
layoutBuilder.addFormButton(Constants.buttonProcessPayment, Constants.rnButton, "cssClass");
layoutBuilder.addFormHidden(Constants.rnSrcFormId, Constants.formIdToolPayment.ToString());
layoutBuilder.addFormHidden(Constants.rnAccountId, AccountId.ToString());
```

### 5. Render the HTML

```csharp
return layoutBuilder.getHtml();
```

## Multi-Step Form Pattern

The example demonstrates a common multi-step form pattern:

### Step 1: Select Account (AccountId == 0)
```csharp
if (AccountId == 0) {
    layoutBuilder.addRow();
    layoutBuilder.rowName = "Select Account";
    layoutBuilder.rowValue = cp.Html.SelectContent(...);
    return layoutBuilder.getHtml();
}
```

### Step 2: Select Payment Method (payMethodId == 0)
```csharp
if (payMethodId == 0) {
    layoutBuilder.addRow();
    layoutBuilder.rowName = "Source of Funds";
    layoutBuilder.rowValue = /* radio buttons for payment methods */;
    return layoutBuilder.getHtml();
}
```

### Step 3: Complete Payment Details
```csharp
// Display selected account
layoutBuilder.addRow();
layoutBuilder.rowName = "Account";
layoutBuilder.rowValue = accountName;

// Display selected payment method
layoutBuilder.addRow();
layoutBuilder.rowName = "Payment Method";
layoutBuilder.rowValue = "Credit Card";

// Show payment-method-specific fields
switch (payMethodId) {
    case CreditCard:
        // Add credit card fields
        break;
    case Cash:
        // Add cash fields
        break;
    // etc.
}

// Add invoices to pay, amounts, etc.
return layoutBuilder.getHtml();
```

## Implementation Pattern Flow

The example in `AccountManagerToolPaymentAddon.cs` follows this flow:

1. **Execute Method** (lines 28-50)
   - Authentication/authorization checks
   - Portal validation
   - Form processing
   - Conditional form display

2. **Process Form** (lines 55-284)
   - Handle Stripe checkout callback (lines 60-147)
   - Parse form data into request object (lines 149-274)
   - Process payment workflow (lines 276-278)
   - Return appropriate form ID

3. **Get Form** (lines 292-773)
   - Initialize LayoutBuilder (line 296-299)
   - Handle success message (lines 346-356)
   - **Multi-step logic:**
     - Step 1: Account selection (lines 361-373)
     - Step 2: Payment method selection (lines 390-437)
     - Step 3: Payment details (lines 443-768)
   - Render and return HTML (line 765-768)

## Key Properties

### Layout Configuration
- `layoutBuilder.title` - Page title
- `layoutBuilder.description` - Description text below title
- `layoutBuilder.includeForm` - Wrap content in form element
- `layoutBuilder.successMessage` - Success message displayed at top

### Row Configuration
- `layoutBuilder.rowName` - Label for the row (left side)
- `layoutBuilder.rowValue` - Content for the row (right side)
- `layoutBuilder.rowHelp` - Optional help text below the value

## Form Submission Pattern

The pattern uses a source form ID to track which form was submitted:

```csharp
// In getForm() - set the source form ID
layoutBuilder.addFormHidden(Constants.rnSrcFormId, Constants.formIdToolPayment.ToString());

// In processForm() - check if form was submitted
int srcFormId = cp.Doc.GetInteger(Constants.rnSrcFormId);
if (srcFormId == 0) {
    return Constants.formIdToolPayment; // Display form
}

// Process the submitted form data
// ...

// Return form ID to display next
return Constants.formIdToolPayment;
```

## Supporting Classes

The example includes stub implementations of supporting classes:

- **ApplicationModel** - Application context wrapper
- **Constants** - Form field names, button values, GUIDs
- **Controllers** - Security, Portal, HTML, Generic, Processing, Workflow
- **Models** - Order, Account, AccountAutoPayOption, EcommerceExport
- **Request Objects** - ProcessPaymentRequest, ProcessPaymentsWorkflowRequest
- **Enums** - PaymentMethodEnum, TransactionPaymentTypeEnum

## Notes

- This example is self-contained for documentation purposes
- Framework classes (CPBaseClass, AddonBaseClass, LayoutBuilderNameValue) are not included
- The code demonstrates the pattern but is not meant to execute standalone
- Multi-step forms use the same form ID but change content based on state
- Hidden fields maintain state between form submissions

## Comparison with LayoutBuilderList

| Feature | LayoutBuilderList | LayoutBuilderNameValue |
|---------|-------------------|------------------------|
| Purpose | Display data tables/lists | Create forms/detail views |
| Layout | Multi-column rows | Name-value pairs |
| Pagination | Yes | No |
| Search | Yes | No |
| Sorting | Yes | No |
| Form Support | Optional | Primary use case |
| Best For | Data grids, reports | Forms, wizards, settings |
