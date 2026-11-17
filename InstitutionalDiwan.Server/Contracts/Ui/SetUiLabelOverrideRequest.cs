namespace InstitutionalDiwan.Server.Contracts.Ui;

/// <summary>
/// Payload submitted to <c>PUT /api/ui-labels/{key}</c>: the replacement text for one built-in UI
/// label. At most 200 characters; placeholders such as <c>{name}</c> must match the built-in
/// default (the front end checks this before saving).
/// </summary>
public sealed record SetUiLabelOverrideRequest(string Value);