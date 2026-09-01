using GotIssues.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GotIssues.Api.UnitTests;

/// <summary>
/// The assumption every enum column with a database default rests on.
///
/// <para>
/// Such a column makes EF decide which CLR value means "unset". The model states
/// `HasSentinel(default)` — zero — which is correct **only while no member of that enum
/// is zero**. Add a `= 0` member and that value silently becomes "unset" and is replaced
/// by the column default on every write.
/// </para>
/// <para>
/// The columns are read from the model rather than listed here. A hand-written list is
/// the defect this ticket spent an acceptance round on, one layer down: add a fourth
/// enum column with a default, forget to add it to the list, and it is unguarded while
/// a green test says otherwise (review N10). Building the model needs no database.
/// </para>
/// <para>
/// Before the sentinel was stated, EF logged `Model.Validation[20601]` on every start to
/// say it was guessing. Stating it silenced that — so this test is the signal that replaced it.
/// </para>
/// <para>
/// <b>It fails `dotnet test`, not the build.</b> An earlier version of this comment said "fails
/// the build", which acceptance measured to be false (G1): with a zero member present,
/// `dotnet build --no-incremental` exits 0. The distinction is the whole point of the warning
/// this replaced — that one was skippable too, and three people skipped it. A test in the
/// sub-millisecond unit tier is a weaker gate than the compiler and a much stronger one than a
/// log line; overstating it would repeat, in the comment on the guard, the exact fault the
/// guard exists to prevent.
/// </para>
/// </summary>
public sealed class IssueLifecycleEnumTests
{
    /// <summary>Every enum property in the model that carries a database default.</summary>
    public static TheoryData<string, Type> EnumColumnsWithDefaults()
    {
        var options = new DbContextOptionsBuilder<GotIssuesDbContext>()
            .UseNpgsql("Host=model-only;Database=model-only")
            .Options;

        using var context = new GotIssuesDbContext(options);

        var data = new TheoryData<string, Type>();

        foreach (var property in context.Model.GetEntityTypes().SelectMany(e => e.GetProperties()))
        {
            var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

            // The annotation, not `GetDefaultValue()`. Acceptance G2 measured that
            // `GetDefaultValue()` returns the CLR default for *any* non-nullable property, so the
            // filter it reads as would be "every non-nullable enum" — a wider set than the
            // description, and one that would fail a future `Severity { None = 0 }` column that
            // has no default and therefore no sentinel problem. The annotation is present only
            // when `HasDefaultValue` was actually called, which is the condition that creates the
            // obligation.
            if (clrType.IsEnum && property.FindAnnotation(RelationalAnnotationNames.DefaultValue) is not null)
            {
                data.Add($"{property.DeclaringType.ShortName()}.{property.Name}", clrType);
            }
        }

        Assert.True(
            data.Count > 0,
            "No enum column with a database default was found. Either the model changed shape or "
            + "this test is no longer looking where the constraint lives — both worth knowing.");

        return data;
    }

    [Theory]
    [MemberData(nameof(EnumColumnsWithDefaults))]
    public void No_member_is_zero_because_zero_means_unset(string column, Type enumType)
    {
        var zeroed = Enum.GetValues(enumType)
            .Cast<object>()
            .Where(value => Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture) == 0)
            .Select(value => value.ToString())
            .ToList();

        Assert.True(
            zeroed.Count == 0,
            $"{enumType.Name} declares {string.Join(", ", zeroed)} as 0, and {column} has a database "
            + "default. Zero is the sentinel for 'unset', so that member would be silently replaced "
            + "by the default on every write. Give it a non-zero value, or drop the database default.");
    }
}
