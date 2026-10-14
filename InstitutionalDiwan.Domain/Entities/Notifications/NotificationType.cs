using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Notifications;

public sealed class NotificationType : Entity<string>
{
    public string Code
    {
        get => Id;
        private set => Id = value;
    }

    public string NameAr { get; private set; } = null!;
    public string TitleTemplateAr { get; private set; } = null!;
    public string BodyTemplateAr { get; private set; } = null!;
    public string DefaultChannelsCsv { get; private set; } = "IN_APP";
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private NotificationType()
    {
    }

    public static Result<NotificationType> Create(
        string? code,
        string? nameAr,
        string? titleTemplateAr,
        string? bodyTemplateAr,
        string defaultChannelsCsv = "IN_APP")
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure<NotificationType>(DomainErrors.Notifications.TypeCodeEmpty);

        if (string.IsNullOrWhiteSpace(titleTemplateAr) || string.IsNullOrWhiteSpace(bodyTemplateAr))
            return Result.Failure<NotificationType>(DomainErrors.Notifications.TitleEmpty);

        var nt = new NotificationType
        {
            Code = code.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            TitleTemplateAr = titleTemplateAr.Trim(),
            BodyTemplateAr = bodyTemplateAr.Trim(),
            DefaultChannelsCsv = defaultChannelsCsv,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(nt);
    }
}
