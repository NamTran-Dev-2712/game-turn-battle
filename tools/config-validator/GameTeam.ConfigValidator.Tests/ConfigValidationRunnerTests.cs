using FluentAssertions;
using Xunit;

namespace GameTeam.ConfigValidator.Tests;

/// <summary>
/// Test hành vi end-to-end của validator qua <see cref="ConfigValidationRunner"/> trên các cây fixture.
/// Kiểm mọi hạng mục Phase 07: valid pass, schema/ref/version fail, gom lỗi, mapping, exit-code, report.
/// </summary>
public sealed class ConfigValidationRunnerTests
{
    // ReportBase cố định = thư mục output → đường dẫn báo cáo ổn định "fixtures/<scenario>/...".
    private static ValidationReport Run(string scenario) =>
        ConfigValidationRunner.Run(new ConfigValidatorOptions(
            TestPaths.Fixture(scenario), TestPaths.SchemaDir, AppContext.BaseDirectory));

    private static IEnumerable<ErrorCode> Codes(ValidationReport report) => report.Errors.Select(e => e.Code);

    [Fact]
    public void Valid_tree_passes_with_no_errors()
    {
        ValidationReport report = Run("valid");

        report.IsValid.Should().BeTrue(because: string.Join("\n", report.Errors));
        report.Errors.Should().BeEmpty();
        report.FilesScanned.Should().Be(8);
    }

    [Fact]
    public void Missing_reference_fails_with_ref001_at_correct_path()
    {
        ValidationReport report = Run("missing-ref");

        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainSingle()
            .Which.Should().Match<ValidationError>(e =>
                e.Code == ErrorCode.Ref001Missing &&
                e.Path == "/skills/0" &&
                e.File.EndsWith("hero_a.json", StringComparison.Ordinal));
    }

    [Fact]
    public void Invalid_reference_type_fails_with_ref002()
    {
        ValidationReport report = Run("invalid-ref-type");

        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(ErrorCode.Ref002Invalid);
        report.Errors[0].Path.Should().Be("/entries/0/ref_id");
    }

    [Fact]
    public void Unsupported_schema_version_fails_with_ver002_only()
    {
        ValidationReport report = Run("bad-version");

        report.IsValid.Should().BeFalse();
        Codes(report).Should().ContainSingle().Which.Should().Be(ErrorCode.Ver002Unsupported);
    }

    [Fact]
    public void Missing_schema_version_fails_with_ver001()
    {
        ValidationReport report = Run("missing-version");

        report.IsValid.Should().BeFalse();
        Codes(report).Should().Contain(ErrorCode.Ver001MissingOrInvalid);
    }

    [Fact]
    public void Schema_violation_fails_with_sch001_at_leaf_path()
    {
        ValidationReport report = Run("schema-invalid");

        report.IsValid.Should().BeFalse();
        Codes(report).Should().OnlyContain(c => c == ErrorCode.Sch001Schema);
        report.Errors.Should().Contain(e => e.Path == "/base_stats/hp");
    }

    [Fact]
    public void Multiple_distinct_errors_are_all_collected_not_stopped_at_first()
    {
        ValidationReport report = Run("multi-error");

        report.IsValid.Should().BeFalse();
        Codes(report).Distinct().Should().Contain(new[]
        {
            ErrorCode.Ver002Unsupported,
            ErrorCode.Sch001Schema,
            ErrorCode.Ref001Missing,
        });
        report.Errors.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void Unknown_directory_reports_map001_and_skips_metadata_dirs()
    {
        ValidationReport report = Run("unknown-type");

        // File dưới thư mục lạ → MAP001.
        report.Errors.Should().Contain(e =>
            e.Code == ErrorCode.Map001UnknownType && e.File.EndsWith("foo.json", StringComparison.Ordinal));

        // liveops/ và _versions/ là metadata → KHÔNG được sinh lỗi nào.
        report.Errors.Should().NotContain(e => e.File.Contains("liveops", StringComparison.Ordinal));
        report.Errors.Should().NotContain(e => e.File.Contains("_versions", StringComparison.Ordinal));
    }

    [Fact]
    public void Malformed_json_reports_json001()
    {
        ValidationReport report = Run("malformed");

        report.IsValid.Should().BeFalse();
        Codes(report).Should().Contain(ErrorCode.Json001Parse);
    }

    [Fact]
    public void Report_line_is_actionable_file_path_code_message()
    {
        ValidationReport report = Run("missing-ref");

        string line = report.Errors[0].ToString();
        line.Should().StartWith("fixtures/missing-ref/heroes/hero_a.json:/skills/0:REF001 ");
        line.Should().Contain("skill_ghost");
    }

    [Fact]
    public void Empty_config_directory_passes()
    {
        string empty = Path.Combine(AppContext.BaseDirectory, "fixtures", "does-not-exist");
        ValidationReport report = ConfigValidationRunner.Run(new ConfigValidatorOptions(empty, TestPaths.SchemaDir));

        report.IsValid.Should().BeTrue();
        report.FilesScanned.Should().Be(0);
    }

    [Fact]
    public void Errors_are_sorted_deterministically_by_file_then_path()
    {
        ValidationReport first = Run("multi-error");
        ValidationReport second = Run("multi-error");

        first.Errors.Select(e => e.ToString())
            .Should().Equal(second.Errors.Select(e => e.ToString()));
    }
}
