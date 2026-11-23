namespace InstitutionalDiwan.Server.Contracts.Attachments;

public sealed record AttachmentOnlyOfficeCallbackRequest(
    string? Key,
    int Status,
    string? Url,
    string? Token);
