using FluentAssertions;
using Xunit;

namespace GameTeam.Codegen.Tests;

public class DeterminismTests
{
    private static string RealJson => File.ReadAllText(TestPaths.RealOpenApi);

    [Fact]
    public void Generate_twice_is_byte_identical()
    {
        IReadOnlyList<GeneratedFile> first = CodegenRunner.Generate(RealJson, "shared/contracts/openapi.json");
        IReadOnlyList<GeneratedFile> second = CodegenRunner.Generate(RealJson, "shared/contracts/openapi.json");

        first.Should().BeEquivalentTo(second, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Files_are_sorted_by_name_ordinal()
    {
        IReadOnlyList<GeneratedFile> files = CodegenRunner.Generate(RealJson, "s");
        files.Select(f => f.FileName).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public void Output_uses_lf_single_trailing_newline_no_trailing_whitespace()
    {
        IReadOnlyList<GeneratedFile> files = CodegenRunner.Generate(RealJson, "s");
        foreach (GeneratedFile file in files)
        {
            file.Content.Should().NotContain("\r", $"{file.FileName} phải dùng LF (không CRLF).");
            file.Content.Should().EndWith("\n", $"{file.FileName} phải kết thúc bằng newline.");
            file.Content.Should().NotEndWith("\n\n", $"{file.FileName} chỉ 1 newline cuối.");

            foreach (string line in file.Content.Split('\n'))
            {
                line.Should().Be(line.TrimEnd(), $"{file.FileName}: dòng không được có khoảng trắng thừa cuối.");
            }
        }
    }

    [Fact]
    public void Run_is_idempotent_and_cleans_stale_files()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "codegen-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);
            // File .gd "stale" không thuộc contract → phải bị xoá.
            File.WriteAllText(Path.Combine(tempDir, "obsolete_dto.gd"), "# stale\n");
            // File không phải .gd → phải giữ nguyên.
            File.WriteAllText(Path.Combine(tempDir, "README.md"), "keep me\n");

            CodegenReport run1 = CodegenRunner.Run(new CodegenOptions(TestPaths.RealOpenApi, tempDir));
            run1.Deleted.Should().Contain("obsolete_dto.gd");
            run1.Written.Should().NotBeEmpty();
            File.Exists(Path.Combine(tempDir, "README.md")).Should().BeTrue();

            Dictionary<string, byte[]> after1 = SnapshotGd(tempDir);

            CodegenReport run2 = CodegenRunner.Run(new CodegenOptions(TestPaths.RealOpenApi, tempDir));
            run2.Deleted.Should().BeEmpty("chạy lại không còn stale.");

            Dictionary<string, byte[]> after2 = SnapshotGd(tempDir);
            after2.Keys.Should().BeEquivalentTo(after1.Keys);
            foreach ((string name, byte[] bytes) in after1)
            {
                after2[name].Should().Equal(bytes, $"{name} phải byte-identical giữa 2 lần chạy.");
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    private static Dictionary<string, byte[]> SnapshotGd(string dir) =>
        Directory.EnumerateFiles(dir, "*.gd")
            .ToDictionary(f => Path.GetFileName(f), File.ReadAllBytes, StringComparer.Ordinal);
}
