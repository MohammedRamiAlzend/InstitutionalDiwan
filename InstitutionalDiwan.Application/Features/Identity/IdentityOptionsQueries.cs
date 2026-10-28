using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity;

// Deliberately exclude personnel contact data, role permissions, and organizational policy fields.
public sealed record DepartmentOptionResponse(Guid DepartmentId, string Code, string NameAr, bool IsActive);
public sealed record RoleOptionResponse(Guid RoleId, string Code, string NameAr, bool IsActive);
public sealed record EmployeeOptionResponse(Guid EmployeeId, string FullNameAr, string? EmployeeNumber, bool IsActive)
{
    public static EmployeeOptionResponse From(Employee employee) =>
        new(employee.EmployeeId, employee.FullNameAr, employee.EmployeeNumber, employee.IsActive);
}
public sealed record UserOptionResponse(Guid UserId, string Username, bool IsActive)
{
    public static UserOptionResponse From(User user) => new(user.UserId, user.Username, user.IsActive);
}

public sealed record ListDepartmentOptionsQuery : IQuery<IReadOnlyCollection<DepartmentOptionResponse>>;
public sealed record ListRoleOptionsQuery : IQuery<IReadOnlyCollection<RoleOptionResponse>>;
public sealed record ListEmployeeOptionsQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IQuery<PagedResult<EmployeeOptionResponse>>;
public sealed record GetEmployeeOptionQuery(Guid EmployeeId) : IQuery<EmployeeOptionResponse>;
public sealed record ListUserOptionsQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IQuery<PagedResult<UserOptionResponse>>;

internal sealed class ListDepartmentOptionsQueryHandler(IDepartmentRepository departments)
    : IQueryHandler<ListDepartmentOptionsQuery, IReadOnlyCollection<DepartmentOptionResponse>>
{
    public async Task<Result<IReadOnlyCollection<DepartmentOptionResponse>>> Handle(
        ListDepartmentOptionsQuery request, CancellationToken cancellationToken) =>
        (await departments.ListAllAsync(cancellationToken))
            .Select(d => new DepartmentOptionResponse(d.DepartmentId, d.Code, d.NameAr, d.IsActive)).ToList();
}

internal sealed class ListRoleOptionsQueryHandler(IRoleRepository roles)
    : IQueryHandler<ListRoleOptionsQuery, IReadOnlyCollection<RoleOptionResponse>>
{
    public async Task<Result<IReadOnlyCollection<RoleOptionResponse>>> Handle(
        ListRoleOptionsQuery request, CancellationToken cancellationToken) =>
        (await roles.ListAllAsync(cancellationToken))
            .Select(r => new RoleOptionResponse(r.RoleId, r.Code, r.NameAr, r.IsActive)).ToList();
}

internal sealed class ListEmployeeOptionsQueryHandler(IEmployeeRepository employees)
    : IQueryHandler<ListEmployeeOptionsQuery, PagedResult<EmployeeOptionResponse>>
{
    public async Task<Result<PagedResult<EmployeeOptionResponse>>> Handle(
        ListEmployeeOptionsQuery request, CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize, request.Search);
        var result = await employees.ListPagedAsync(page, cancellationToken: cancellationToken);
        return new PagedResult<EmployeeOptionResponse>(result.Items.Select(EmployeeOptionResponse.From).ToList(),
            page.NormalizedPage, page.NormalizedPageSize, result.TotalCount);
    }
}

internal sealed class ListUserOptionsQueryHandler(IUserRepository users)
    : IQueryHandler<ListUserOptionsQuery, PagedResult<UserOptionResponse>>
{
    public async Task<Result<PagedResult<UserOptionResponse>>> Handle(
        ListUserOptionsQuery request, CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize, request.Search);
        var result = await users.ListPagedAsync(page, cancellationToken);
        return new PagedResult<UserOptionResponse>(result.Items.Select(UserOptionResponse.From).ToList(),
            page.NormalizedPage, page.NormalizedPageSize, result.TotalCount);
    }
}

internal sealed class GetEmployeeOptionQueryHandler(IEmployeeRepository employees)
    : IQueryHandler<GetEmployeeOptionQuery, EmployeeOptionResponse>
{
    public async Task<Result<EmployeeOptionResponse>> Handle(
        GetEmployeeOptionQuery request, CancellationToken cancellationToken)
    {
        var employee = await employees.GetByIdAsync(request.EmployeeId, cancellationToken);
        return employee is null
            ? Result.Failure<EmployeeOptionResponse>(DomainErrors.Identity.EmployeeNotFound)
            : EmployeeOptionResponse.From(employee);
    }
}
