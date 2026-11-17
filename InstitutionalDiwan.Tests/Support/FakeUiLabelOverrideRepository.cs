using InstitutionalDiwan.Domain.Entities.Ui;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Tests.Support;

internal sealed class FakeUiLabelOverrideRepository : IUiLabelOverrideRepository
{
    public List<UiLabelOverride> Overrides { get; } = new();

    public Task<IReadOnlyList<UiLabelOverride>> ListAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<UiLabelOverride>>(
            Overrides.OrderBy(labelOverride => labelOverride.Key).ToList());

    public Task<UiLabelOverride?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(Overrides.FirstOrDefault(labelOverride => labelOverride.Key == key));

    public void Add(UiLabelOverride labelOverride) => Overrides.Add(labelOverride);

    public void Remove(UiLabelOverride labelOverride) => Overrides.Remove(labelOverride);
}