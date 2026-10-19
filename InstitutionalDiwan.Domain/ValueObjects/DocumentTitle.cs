namespace InstitutionalDiwan.Domain.ValueObjects;

public sealed class DocumentTitle : ValueObject
{
    public string Value { get; }

    private DocumentTitle(string value)
    {
        Value = value;
    }

    public static Result<DocumentTitle> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<DocumentTitle>(DomainErrors.Correspondence.TitleEmpty);

        var trimmed = value.Trim();
        if (trimmed.Length > 500)
            return Result.Failure<DocumentTitle>(DomainErrors.Correspondence.TitleTooLong);

        return Result.Success(new DocumentTitle(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
