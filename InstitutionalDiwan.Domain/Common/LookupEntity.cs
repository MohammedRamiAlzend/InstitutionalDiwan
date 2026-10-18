using System;

namespace InstitutionalDiwan.Domain.Common;

/// <summary>
/// Base class for small, seedable reference ("lookup") tables. Every lookup is keyed by an integer
/// id and carries an English (<see cref="Desc"/>) and Arabic (<see cref="DescAr"/>) label, so the
/// domain never stores free-form status/type strings — dependent tables hold an integer foreign key
/// into the lookup instead. See <c>Entities/Lookups</c> and the per-table seeders.
/// </summary>
public abstract class LookupEntity : Entity<int>
{
    /// <summary>English label.</summary>
    public string Desc { get; protected set; } = null!;

    /// <summary>Arabic label.</summary>
    public string DescAr { get; protected set; } = null!;

    /// <summary>Whether the entry may still be referenced by new records.</summary>
    public bool IsActive { get; protected set; } = true;

    public DateTime CreatedAt { get; protected set; }

    protected LookupEntity()
    {
    }

    /// <summary>Initializes a brand-new lookup entry with its stable id and labels.</summary>
    protected LookupEntity(int id, string desc, string descAr)
    {
        Id = id;
        Desc = desc.Trim();
        DescAr = descAr.Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Updates the labels (used by lookup CRUD endpoints).</summary>
    public void Relabel(string desc, string descAr)
    {
        Desc = desc.Trim();
        DescAr = descAr.Trim();
    }

    /// <summary>Soft-deletes the entry so it can no longer be selected, without breaking existing FKs.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Re-enables a previously deactivated entry.</summary>
    public void Activate() => IsActive = true;
}
