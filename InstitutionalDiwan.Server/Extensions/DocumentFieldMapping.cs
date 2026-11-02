using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Server.Contracts.Correspondence;

namespace InstitutionalDiwan.Server.Extensions;

internal static class DocumentFieldMapping
{
    public static DocumentFieldSettingsInput ToInput(this DocumentFieldSettingsRequest request) => new(
        request.LabelAr,
        request.IsRequired,
        request.DisplayOrder,
        request.RegexPattern,
        request.RegexErrorMessageAr,
        request.MinNumber,
        request.MaxNumber,
        request.MaxLength,
        request.Options?.Select(option => new DocumentFieldOption(option.Key, option.LabelAr)).ToList());

    public static IReadOnlyList<DocumentFieldValueInput>? ToInputs(
        this IReadOnlyList<DocumentFieldValueRequest>? values) =>
        values?.Select(value => new DocumentFieldValueInput(value.FieldKey, value.Value)).ToList();
}
