using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;

/// <summary>
/// The administrator-editable configuration of a field inside one schema version. The data type is
/// not part of it: it belongs to the field's stable identity and is applied from there.
/// </summary>
public sealed record DocumentFieldSettingsInput(
    string LabelAr,
    bool IsRequired = false,
    int DisplayOrder = 0,
    string? RegexPattern = null,
    string? RegexErrorMessageAr = null,
    decimal? MinNumber = null,
    decimal? MaxNumber = null,
    int? MaxLength = null,
    IReadOnlyList<DocumentFieldOption>? Options = null)
{
    /// <summary>Validates the input against <paramref name="dataType"/> and builds the domain settings.</summary>
    public Result<DocumentFieldSettings> ToSettings(DocumentFieldDataType dataType) =>
        DocumentFieldSettings.Create(
            dataType, LabelAr, IsRequired, DisplayOrder, RegexPattern, RegexErrorMessageAr,
            MinNumber, MaxNumber, MaxLength, Options);
}
