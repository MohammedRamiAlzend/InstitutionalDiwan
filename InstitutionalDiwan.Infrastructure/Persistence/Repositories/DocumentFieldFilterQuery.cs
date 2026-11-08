using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

/// <summary>The dynamic-field filter predicate shared by the document registry and the archive search.</summary>
internal static class DocumentFieldFilterQuery
{
    /// <summary>
    /// Keeps documents that store a value for the filtered field satisfying the comparison. Dates are
    /// compared by calendar day: <c>eq</c> matches the whole day, <c>gt</c> starts after it and
    /// <c>lte</c> ends with it. A multi-select matches when the option is among its choices.
    /// </summary>
    public static IQueryable<Document> WhereFieldMatches(
        this IQueryable<Document> query,
        DiwanDbContext context,
        DocumentFieldFilter filter)
    {
        var fieldId = filter.DocumentFieldId;
        Expression<Func<DocumentFieldValue, bool>> predicate;

        switch (filter.DataType)
        {
            case DocumentFieldDataType.Text:
            case DocumentFieldDataType.LongText:
            {
                var text = filter.Text!;
                if (filter.Operator == FieldFilterOperator.Contains)
                {
                    var pattern = $"%{EscapeLike(text)}%";
                    predicate = v => v.TextValue != null && EF.Functions.Like(v.TextValue, pattern, "\\");
                }
                else
                {
                    predicate = v => v.TextValue == text;
                }
                break;
            }

            case DocumentFieldDataType.Number:
            {
                var number = filter.Number!.Value;
                predicate = filter.Operator switch
                {
                    FieldFilterOperator.GreaterThan => v => v.NumberValue > number,
                    FieldFilterOperator.GreaterThanOrEqual => v => v.NumberValue >= number,
                    FieldFilterOperator.LessThan => v => v.NumberValue < number,
                    FieldFilterOperator.LessThanOrEqual => v => v.NumberValue <= number,
                    _ => v => v.NumberValue == number
                };
                break;
            }

            case DocumentFieldDataType.Date:
            {
                var dayStart = filter.Date!.Value.Date;
                var dayEnd = dayStart.AddDays(1);
                predicate = filter.Operator switch
                {
                    FieldFilterOperator.GreaterThan => v => v.DateValue >= dayEnd,
                    FieldFilterOperator.GreaterThanOrEqual => v => v.DateValue >= dayStart,
                    FieldFilterOperator.LessThan => v => v.DateValue < dayStart,
                    FieldFilterOperator.LessThanOrEqual => v => v.DateValue < dayEnd,
                    _ => v => v.DateValue >= dayStart && v.DateValue < dayEnd
                };
                break;
            }

            case DocumentFieldDataType.Boolean:
            {
                var boolean = filter.Boolean!.Value;
                predicate = v => v.BoolValue == boolean;
                break;
            }

            case DocumentFieldDataType.MultiSelect:
            {
                var pattern = $"%|{EscapeLike(filter.OptionKey!)}|%";
                predicate = v => v.TextValue != null && EF.Functions.Like(v.TextValue, pattern, "\\");
                break;
            }

            default:
            {
                var optionKey = filter.OptionKey!;
                predicate = v => v.OptionKey == optionKey;
                break;
            }
        }

        return query.Where(document => context.DocumentFieldValues
            .Where(value => value.DocumentFieldId == fieldId)
            .Where(predicate)
            .Any(value => value.DocumentId == document.DocumentId));
    }

    /// <summary>Escapes the LIKE wildcards so a filter for "100%" matches the literal text.</summary>
    private static string EscapeLike(string term) =>
        term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
