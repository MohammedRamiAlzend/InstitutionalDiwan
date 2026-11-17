using InstitutionalDiwan.Domain.Entities.Ui;

namespace InstitutionalDiwan.Application.Features.Ui.UiLabels;

/// <summary>
/// All stored UI label replacements plus a cache version. <see cref="Version"/> is the tick count
/// of the newest write (0 when nothing is overridden); clients keep it to detect changes.
/// </summary>
public sealed record UiLabelOverridesResponse(
    long Version,
    IReadOnlyDictionary<string, string> Overrides)
{
    public static UiLabelOverridesResponse From(IReadOnlyList<UiLabelOverride> overrides)
    {
        var version = overrides.Count == 0 ? 0L : overrides.Max(labelOverride => labelOverride.UpdatedAt.Ticks);
        var map = overrides.ToDictionary(labelOverride => labelOverride.Key, labelOverride => labelOverride.Value);
        return new UiLabelOverridesResponse(version, map);
    }
}