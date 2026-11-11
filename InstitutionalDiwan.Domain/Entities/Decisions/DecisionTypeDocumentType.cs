namespace InstitutionalDiwan.Domain.Entities.Decisions;

/// <summary>
/// One document type a <see cref="DecisionType"/> may be issued on. A type with no rows is defined
/// but cannot be issued.
/// </summary>
public sealed class DecisionTypeDocumentType
{
    public Guid DecisionTypeId { get; private set; }
    public int DocumentTypeId { get; private set; }

    private DecisionTypeDocumentType()
    {
    }

    internal DecisionTypeDocumentType(Guid decisionTypeId, int documentTypeId)
    {
        DecisionTypeId = decisionTypeId;
        DocumentTypeId = documentTypeId;
    }
}
