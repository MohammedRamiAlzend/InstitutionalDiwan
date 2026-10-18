using System;
using System.Collections.Generic;
using System.Linq;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Domain.Entities.Attachments;

public sealed class DocumentFile : AggregateRoot<Guid>
{
    private readonly List<DocumentFileVersion> _versions = new();

    public Guid FileId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → Document.DocumentId (InstitutionalDiwan.Domain.Entities.Correspondence.Document)
    public Guid DocumentId { get; private set; }
    // FK → DocumentFileType.Code (InstitutionalDiwan.Domain.Entities.Attachments.DocumentFileType)
    public string FileTypeCode { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public bool IsPrimaryScan { get; private set; }
    public int CurrentVersionNumber { get; private set; }
    public bool IsArchived { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<DocumentFileVersion> Versions => _versions.AsReadOnly();

    private DocumentFile()
    {
    }

    public static Result<DocumentFile> Create(
        Guid documentId,
        string? fileTypeCode,
        string? title,
        Guid createdByUserId,
        bool isPrimaryScan = false)
    {
        if (string.IsNullOrWhiteSpace(fileTypeCode))
            return Result.Failure<DocumentFile>(DomainErrors.Attachments.FileTypeEmpty);

        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<DocumentFile>(DomainErrors.Attachments.TitleEmpty);

        var file = new DocumentFile
        {
            FileId = Guid.NewGuid(),
            DocumentId = documentId,
            FileTypeCode = fileTypeCode.Trim().ToUpperInvariant(),
            Title = title.Trim(),
            IsPrimaryScan = isPrimaryScan,
            CurrentVersionNumber = 0,
            IsArchived = false,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        file.RaiseDomainEvent(new DocumentFileCreatedEvent(file.FileId, file.DocumentId, file.FileTypeCode, file.Title));
        return Result.Success(file);
    }

    public Result<DocumentFileVersion> AddVersion(
        string storageProviderCode,
        string? relativePath,
        string? originalFileName,
        string? mimeType,
        long fileSizeBytes,
        string? checksumSha256,
        Guid uploadedByUserId,
        string? changeNotes = null)
    {
        if (IsArchived)
            return Result.Failure<DocumentFileVersion>(DomainErrors.Attachments.FileArchived);

        var nextVersionNo = CurrentVersionNumber + 1;
        var versionResult = DocumentFileVersion.Create(
            FileId,
            nextVersionNo,
            storageProviderCode,
            relativePath,
            originalFileName,
            mimeType,
            fileSizeBytes,
            checksumSha256,
            uploadedByUserId,
            changeNotes);

        if (versionResult.IsFailure)
            return versionResult;

        foreach (var existing in _versions.Where(v => v.IsCurrent))
            existing.MarkSuperseded();

        _versions.Add(versionResult.Value);
        CurrentVersionNumber = nextVersionNo;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new DocumentFileVersionUploadedEvent(
            FileId,
            versionResult.Value.VersionId,
            DocumentId,
            nextVersionNo,
            versionResult.Value.ChecksumSha256));

        return versionResult;
    }

    public Result Archive()
    {
        if (IsArchived) return Result.Success();
        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new DocumentFileArchivedEvent(FileId, DocumentId));
        return Result.Success();
    }
}
