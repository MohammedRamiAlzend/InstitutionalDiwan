namespace InstitutionalDiwan.Server.Contracts.Lookups;

/// <summary>Payload submitted to <c>POST /api/lookups/{lookup}</c>.</summary>
public sealed record CreateLookupEntryRequest(
    int Id,
    string Desc,
    string DescAr);

/// <summary>Payload submitted to <c>PUT /api/lookups/{lookup}/{id}</c>.</summary>
public sealed record RelabelLookupEntryRequest(
    string Desc,
    string DescAr);
