using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;

namespace InstitutionalDiwan.Tests.Application;

public sealed class DocumentDisplayNameTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly DateTime Created = new(2026, 3, 9, 8, 0, 0, DateTimeKind.Utc);

    private static Document Draft() => Document.CreateDraft(
        null, DocumentType.InternalMemoId, null, Owner, Actor,
        utcNow: Created,
        displayName: Document.BuildDisplayName("مذكرة", null, Created)).Value;

    private static void AddLine(Document document, Guid department, string number, int year) =>
        Assert.True(document.AddDepartmentNumber(DocumentNumber.CreateDepartmentEntry(
            document.DocumentId, department, Guid.NewGuid(), number, new DateTime(year, 1, 5), Actor, null, utcNow: Created).Value).IsSuccess);

    [Fact]
    public void Draft_IsNamedByTypeAndDate_WithoutATitle()
    {
        var document = Draft();

        Assert.Equal("مذكرة · 2026-03-09", document.DisplayName);
        Assert.Null(document.Title);
    }

    [Fact]
    public void FirstLineInOwnRegister_GivesTheName()
    {
        var document = Draft();
        AddLine(document, Owner, "1447/2026", 2026);

        Assert.Equal("مذكرة · 1447/2026", document.DisplayName);
    }

    [Fact]
    public void LaterLinesAndOtherDepartments_LeaveTheNameAlone()
    {
        var document = Draft();
        AddLine(document, Guid.NewGuid(), "5/2026", 2026);
        Assert.Equal("مذكرة · 2026-03-09", document.DisplayName);

        AddLine(document, Owner, "1447/2026", 2026);
        AddLine(document, Owner, "9/2027", 2027);
        Assert.Equal("مذكرة · 1447/2026", document.DisplayName);
    }

    [Fact]
    public void Create_RejectsADocumentWithNothingToNameIt()
    {
        var result = Document.CreateDraft(null, DocumentType.InternalMemoId, null, Owner, Actor);

        Assert.Equal("Correspondence.DisplayNameEmpty", result.Error.Code);
    }
}
