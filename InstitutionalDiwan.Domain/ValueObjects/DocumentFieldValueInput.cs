namespace InstitutionalDiwan.Domain.ValueObjects;

/// <summary>
/// One raw dynamic-field value as submitted by a caller: the field key and the value as text
/// (numbers invariant-culture, dates ISO 8601, booleans <c>true</c>/<c>false</c>, selects the option
/// key). A null or blank <paramref name="Value"/> means "no value" and is not stored.
/// </summary>
public sealed record DocumentFieldValueInput(string FieldKey, string? Value);
