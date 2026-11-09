namespace InstitutionalDiwan.Domain.Entities.Correspondence;

/// <summary>
/// Places a <c>Person</c> on a document in a given capacity. A person appears at most once per document.
/// </summary>
public sealed class DocumentPerson : Entity<Guid>
{
    public Guid DocumentPersonId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → Document.DocumentId (InstitutionalDiwan.Domain.Entities.Correspondence.Document)
    public Guid DocumentId { get; private set; }
    // FK → Person.PersonId (InstitutionalDiwan.Domain.Entities.Persons.Person)
    public Guid PersonId { get; private set; }
    // FK → DocumentPersonRole.Id (InstitutionalDiwan.Domain.Entities.Persons.DocumentPersonRole)
    public int RoleId { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private DocumentPerson()
    {
    }

    internal static DocumentPerson Create(Guid documentId, Guid personId, int roleId, Guid createdByUserId, DateTime utcNow) => new()
    {
        DocumentPersonId = Guid.NewGuid(),
        DocumentId = documentId,
        PersonId = personId,
        RoleId = roleId,
        CreatedByUserId = createdByUserId,
        CreatedAt = utcNow,
    };

    internal void ChangeRole(int roleId) => RoleId = roleId;
}
