using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Attachments;

/// <summary>Opens one version of a document file for download; omit the version to get the current one.</summary>
public sealed record DownloadDocumentFileVersionQuery(
    Guid FileId,
    Guid UserId,
    Guid? VersionId = null) : IQuery<DocumentFileDownload>;
