using System;

namespace InstitutionalDiwan.Domain.Common;

public sealed class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string message, string code = "DOMAIN_RULE_VIOLATION")
        : base(message)
    {
        Code = code;
    }

    public DomainException(Error error)
        : base(error.Description)
    {
        Code = error.Code;
    }
}
