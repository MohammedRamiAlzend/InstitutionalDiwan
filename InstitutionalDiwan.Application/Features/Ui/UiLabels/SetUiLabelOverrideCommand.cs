using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Ui;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Ui.UiLabels;

/// <summary>
/// Creates or updates the replacement for one UI label key. Returns the full override set so the
/// caller can refresh its cache in one round trip.
/// </summary>
public sealed record SetUiLabelOverrideCommand(string Key, string Value, Guid? UpdatedByUserId)
    : ICommand<UiLabelOverridesResponse>;

internal sealed class SetUiLabelOverrideCommandHandler
    : ICommandHandler<SetUiLabelOverrideCommand, UiLabelOverridesResponse>
{
    private readonly IUiLabelOverrideRepository _labelOverrideRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetUiLabelOverrideCommandHandler(
        IUiLabelOverrideRepository labelOverrideRepository,
        IUnitOfWork unitOfWork)
    {
        _labelOverrideRepository = labelOverrideRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UiLabelOverridesResponse>> Handle(
        SetUiLabelOverrideCommand request,
        CancellationToken cancellationToken)
    {
        var key = UiLabelOverride.NormalizeKey(request.Key);
        if (key is null)
            return Result.Failure<UiLabelOverridesResponse>(DomainErrors.UiLabel.KeyInvalid);

        var existing = await _labelOverrideRepository.GetByKeyAsync(key, cancellationToken);
        if (existing is null)
        {
            var created = UiLabelOverride.Create(key, request.Value, request.UpdatedByUserId, _unitOfWork.UtcNow);
            if (created.IsFailure)
                return Result.Failure<UiLabelOverridesResponse>(created.Error);

            _labelOverrideRepository.Add(created.Value);
        }
        else
        {
            var updated = existing.UpdateValue(request.Value, request.UpdatedByUserId, _unitOfWork.UtcNow);
            if (updated.IsFailure)
                return Result.Failure<UiLabelOverridesResponse>(updated.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var overrides = await _labelOverrideRepository.ListAllAsync(cancellationToken);
        return UiLabelOverridesResponse.From(overrides);
    }
}