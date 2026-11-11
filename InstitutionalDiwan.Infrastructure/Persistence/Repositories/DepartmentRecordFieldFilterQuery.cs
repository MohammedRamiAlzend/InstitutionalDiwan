using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

/// <summary>The department-record counterpart of <see cref="DocumentFieldFilterQuery"/>, for a department's register page.</summary>
internal static class DepartmentRecordFieldFilterQuery
{
    /// <summary>
    /// Keeps register lines whose latest visit on the form version stores a value for the filtered field
    /// that satisfies the comparison, the same visit whose values the page shows. Dates compare by
    /// calendar day; a multi-select matches when the option is among its choices.
    /// </summary>
    public static IQueryable<DocumentNumber> WhereRecordFieldMatches(
        this IQueryable<DocumentNumber> lines,
        DiwanDbContext context,
        Guid schemaVersionId,
        DocumentFieldFilter filter)
    {
        var fieldId = filter.DocumentFieldId;
        var predicate = Predicate(filter);
        var visits = context.DocumentRegistryVisits.Where(visit => visit.SchemaVersionId == schemaVersionId);

        return lines.Where(number => context.DepartmentRecordValues
            .Where(value => value.DocumentFieldId == fieldId)
            .Where(predicate)
            .Any(value => visits.Any(visit =>
                visit.VisitId == value.VisitId &&
                visit.DocumentNumberId == number.DocumentNumberId &&
                !visits.Any(later =>
                    later.DocumentNumberId == visit.DocumentNumberId &&
                    (later.RegistryDate > visit.RegistryDate ||
                     (later.RegistryDate == visit.RegistryDate && later.RecordedAt > visit.RecordedAt))))));
    }

    private static Expression<Func<DepartmentRecordValue, bool>> Predicate(DocumentFieldFilter filter)
    {
        switch (filter.DataType)
        {
            case DocumentFieldDataType.Text:
            case DocumentFieldDataType.LongText:
            {
                var text = filter.Text!;
                if (filter.Operator != FieldFilterOperator.Contains)
                    return v => v.TextValue == text;

                var pattern = $"%{EscapeLike(text)}%";
                return v => v.TextValue != null && EF.Functions.Like(v.TextValue, pattern, "\\");
            }

            case DocumentFieldDataType.Number:
            {
                var number = filter.Number!.Value;
                return filter.Operator switch
                {
                    FieldFilterOperator.GreaterThan => v => v.NumberValue > number,
                    FieldFilterOperator.GreaterThanOrEqual => v => v.NumberValue >= number,
                    FieldFilterOperator.LessThan => v => v.NumberValue < number,
                    FieldFilterOperator.LessThanOrEqual => v => v.NumberValue <= number,
                    _ => v => v.NumberValue == number
                };
            }

            case DocumentFieldDataType.Date:
            {
                var dayStart = filter.Date!.Value.Date;
                var dayEnd = dayStart.AddDays(1);
                return filter.Operator switch
                {
                    FieldFilterOperator.GreaterThan => v => v.DateValue >= dayEnd,
                    FieldFilterOperator.GreaterThanOrEqual => v => v.DateValue >= dayStart,
                    FieldFilterOperator.LessThan => v => v.DateValue < dayStart,
                    FieldFilterOperator.LessThanOrEqual => v => v.DateValue < dayEnd,
                    _ => v => v.DateValue >= dayStart && v.DateValue < dayEnd
                };
            }

            case DocumentFieldDataType.Boolean:
            {
                var boolean = filter.Boolean!.Value;
                return v => v.BoolValue == boolean;
            }

            case DocumentFieldDataType.MultiSelect:
            {
                var pattern = $"%|{EscapeLike(filter.OptionKey!)}|%";
                return v => v.TextValue != null && EF.Functions.Like(v.TextValue, pattern, "\\");
            }

            default:
            {
                var optionKey = filter.OptionKey!;
                return v => v.OptionKey == optionKey;
            }
        }
    }

    private static string EscapeLike(string term) =>
        term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
