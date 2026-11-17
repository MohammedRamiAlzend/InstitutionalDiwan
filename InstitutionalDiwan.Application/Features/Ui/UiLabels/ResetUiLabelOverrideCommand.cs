using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Ui;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Ui.UiLabels;

/// <summary>
/// Removes the replacement for one UI label key so the built-in default applies again. Resetting
/// a key that was never overridden is a successful no-op.
/// </summary>
public sealed record ResetUiLabelOverrideCommand(string Key) : ICommand<UiLabelOverridesResponse>;

internal sealed class ResetUiLabelOverrideCommandHandler
    : ICommandHandler<ResetUiLabelOverrideCommand, UiLabelOverridesResponse>
{
    private readonly IUiLabelOverrideRepository _labelOverrideRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ResetUiLabelOverrideCommandHandler(
        IUiLabelOverrideRepository labelOverrideRepository,
        IUnitOfWork unitOfWork)
    {
        _labelOverrideRepository = labelOverrideRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UiLabelOverridesResponse>> Handle(
        ResetUiLabelOverrideCommand request,
        CancellationToken cancellationToken)
    {
        var key = UiLabelOverride.NormalizeKey(request.Key);
        var existing = key is null
            ? null
            : await _labelOverrideRepository.GetByKeyAsync(key, cancellationToken);

        if (existing is not null)
        {
            _labelOverrideRepository.Remove(existing);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var overrides = await _labelOverrideRepository.ListAllAsync(cancellationToken);
        return UiLabelOverridesResponse.From(overrides);
    }
}