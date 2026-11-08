namespace InstitutionalDiwan.Domain.Entities.Numbering;

/// <summary>
/// One arrival of a document in a department's register: the department's number stays on the
/// parent <see cref="DocumentNumber"/>, and each visit adds a date. A date can appear only once per
/// register line.
/// </summary>
public sealed class DocumentRegistryVisit : Entity<Guid>
{
    public Guid VisitId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → DocumentNumber.DocumentNumberId (InstitutionalDiwan.Domain.Entities.Numbering.DocumentNumber)
    public Guid DocumentNumberId { get; private set; }
    /// <summary>Date written in the department register for this visit (no time part).</summary>
    public DateTime RegistryDate { get; private set; }
    // FK → DocumentMovementStep.StepId — null until a receipt claims this visit
    public Guid? MovementStepId { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid RecordedByUserId { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public string? Notes { get; private set; }

    private DocumentRegistryVisit()
    {
    }

    internal static DocumentRegistryVisit Create(
        Guid documentNumberId,
        DateTime registryDate,
        Guid recordedByUserId,
        Guid? movementStepId,
        string? notes,
        DateTime recordedAt)
    {
        return new DocumentRegistryVisit
        {
            VisitId = Guid.NewGuid(),
            DocumentNumberId = documentNumberId,
            RegistryDate = registryDate.Date,
            MovementStepId = movementStepId,
            RecordedByUserId = recordedByUserId,
            RecordedAt = recordedAt,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }

    internal void LinkToStep(Guid movementStepId) => MovementStepId = movementStepId;
}
