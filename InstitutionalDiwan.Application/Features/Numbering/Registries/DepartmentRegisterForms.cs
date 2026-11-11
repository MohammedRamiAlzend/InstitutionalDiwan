using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

/// <summary>The record forms a department has issued, grouped by document type, newest version first.</summary>
public sealed record GetDepartmentRegisterFormsQuery(Guid DepartmentId) : IQuery<DepartmentRegisterFormsResponse>;

/// <summary>The record forms of one department. Empty when it has none published.</summary>
public sealed record DepartmentRegisterFormsResponse(IReadOnlyList<DepartmentRegisterFormResponse> Forms);

/// <summary>
/// The record form of one document type for the department. <c>Versions</c> lists every published or
/// retired version, newest first, so the first one is the latest and is what a screen should open on.
/// </summary>
public sealed record DepartmentRegisterFormResponse(
    int DocumentTypeId,
    IReadOnlyList<SchemaVersionResponse> Versions);

internal sealed class GetDepartmentRegisterFormsQueryHandler
    : IQueryHandler<GetDepartmentRegisterFormsQuery, DepartmentRegisterFormsResponse>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;
    private readonly IDocumentFieldOptionResolver _optionResolver;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;

    public GetDepartmentRegisterFormsQueryHandler(
        IDepartmentRepository departmentRepository,
        IDocumentTypeSchemaVersionRepository versionRepository,
        IDocumentFieldOptionResolver optionResolver,
        IDepartmentScopeEvaluator departmentScopeEvaluator)
    {
        _departmentRepository = departmentRepository;
        _versionRepository = versionRepository;
        _optionResolver = optionResolver;
        _departmentScopeEvaluator = departmentScopeEvaluator;
    }

    public async Task<Result<DepartmentRegisterFormsResponse>> Handle(
        GetDepartmentRegisterFormsQuery request,
        CancellationToken cancellationToken)
    {
        if (await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken) is null)
            return Result.Failure<DepartmentRegisterFormsResponse>(DomainErrors.Department.NotFound);

        if (!await _departmentScopeEvaluator.CanAccessDepartmentAsync(request.DepartmentId, cancellationToken))
        {
            return Result.Failure<DepartmentRegisterFormsResponse>(
                DomainErrors.Correspondence.DepartmentOutOfScope);
        }

        var versions = await _versionRepository.ListIssuedByDepartmentAsync(request.DepartmentId, cancellationToken);

        var forms = new List<DepartmentRegisterFormResponse>();
        foreach (var group in versions.GroupBy(version => version.DocumentTypeId).OrderBy(group => group.Key))
        {
            var responses = new List<SchemaVersionResponse>();
            foreach (var version in group.OrderByDescending(candidate => candidate.VersionNumber))
            {
                var options = await _optionResolver.ResolveAsync(version, cancellationToken);
                responses.Add(SchemaVersionResponse.From(version, options));
            }

            forms.Add(new DepartmentRegisterFormResponse(group.Key, responses));
        }

        return Result.Success(new DepartmentRegisterFormsResponse(forms));
    }
}
