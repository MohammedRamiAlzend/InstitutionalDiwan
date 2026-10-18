using System;
using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Domain.Entities.Numbering;

public sealed class NumberingCounter : Entity<Guid>
{
    public Guid CounterId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → NumberingRegistry.RegistryId (InstitutionalDiwan.Domain.Entities.Numbering.NumberingRegistry)
    public Guid RegistryId { get; private set; }
    public int NumberingYear { get; private set; }
    public long LastValue { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public byte[]? RowVersion { get; private set; }

    private NumberingCounter()
    {
    }

    internal static NumberingCounter Create(Guid registryId, int numberingYear)
    {
        return new NumberingCounter
        {
            CounterId = Guid.NewGuid(),
            RegistryId = registryId,
            NumberingYear = numberingYear,
            LastValue = 0,
            UpdatedAt = DateTime.UtcNow
        };
    }

    internal long Increment()
    {
        LastValue += 1;
        UpdatedAt = DateTime.UtcNow;
        return LastValue;
    }
}
