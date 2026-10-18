using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Correspondence;

/// <summary>
/// Reference table of document directions (incoming, outgoing, internal). <see cref="AllId"/> is the
/// sentinel used by <see cref="DocumentType.AllowedDirectionId"/> to mean "any direction".
/// Documents store the integer id, not the former free-form direction string.
/// </summary>
public sealed class DocumentDirection : LookupEntity
{
    public const int InternalId = 1;
    public const int IncomingId = 2;
    public const int OutgoingId = 3;
    public const int AllId = 4;

    private DocumentDirection()
    {
    }

    private DocumentDirection(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<DocumentDirection> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<DocumentDirection>(DomainErrors.Correspondence.DirectionInvalid);

        return Result.Success(new DocumentDirection(id, desc, descAr));
    }
}
