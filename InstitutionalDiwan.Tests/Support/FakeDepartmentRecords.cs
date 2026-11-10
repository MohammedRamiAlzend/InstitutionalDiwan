using InstitutionalDiwan.Application.Features.DepartmentRecords;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>Builds the department-record service over in-memory repositories for handler tests.</summary>
internal static class FakeDepartmentRecords
{
    public static DepartmentRecordService Service(
        FakeDocumentRepository documents,
        FakeDocumentTypeSchemaVersionRepository? versions = null,
        FakeDocumentFieldOptionResolver? options = null) =>
        new(documents, versions ?? new FakeDocumentTypeSchemaVersionRepository(), options ?? new FakeDocumentFieldOptionResolver());
}
