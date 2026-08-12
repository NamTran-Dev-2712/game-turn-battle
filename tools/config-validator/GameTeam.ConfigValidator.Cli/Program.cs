using GameTeam.ConfigValidator;

// CLI MỎNG: parse args → gọi core → in report (file:path:CODE) → exit code.
// KHÔNG chứa logic validate (nằm trong GameTeam.ConfigValidator — ranh giới tái dùng Phase 21).
//
// Exit codes:
//   0 = hợp lệ (không lỗi)
//   1 = có lỗi validate (schema / reference / version / mapping / parse)
//   2 = lỗi sử dụng hoặc hạ tầng tool (sai tham số, thiếu thư mục schema)

const int ExitOk = 0;
const int ExitValidationFailed = 1;
const int ExitUsageOrToolError = 2;

string[] positional = args.Where(a => !a.StartsWith('-')).ToArray();

if (args.Contains("-h") || args.Contains("--help") || positional.Length == 0)
{
    PrintUsage();
    return positional.Length == 0 && !args.Contains("-h") && !args.Contains("--help")
        ? ExitUsageOrToolError
        : ExitOk;
}

if (positional.Length is < 1 or > 2)
{
    Console.Error.WriteLine("Lỗi: cần 1–2 tham số vị trí: <config-dir> [schema-dir].");
    PrintUsage();
    return ExitUsageOrToolError;
}

string configDir = positional[0];
string schemaDir = positional.Length == 2 ? positional[1] : "shared/config-schema";

try
{
    ValidationReport report = ConfigValidationRunner.Run(new ConfigValidatorOptions(configDir, schemaDir));

    if (report.IsValid)
    {
        Console.WriteLine($"OK: {report.FilesScanned} file config hợp lệ (schema + reference + version).");
        return ExitOk;
    }

    Console.Error.WriteLine($"FAIL: {report.Errors.Count} lỗi trong {report.FilesScanned} file config.");
    Console.Error.WriteLine("Định dạng: file:jsonpath:CODE message  (giải thích mã: tools/config-validator/README.md)");
    foreach (ValidationError error in report.Errors)
    {
        Console.Error.WriteLine(error.ToString());
    }

    return ExitValidationFailed;
}
catch (InvalidOperationException ex)
{
    // Lỗi hạ tầng tool (vd thiếu thư mục/schema) — phân biệt với lỗi config.
    Console.Error.WriteLine($"Lỗi tool: {ex.Message}");
    return ExitUsageOrToolError;
}

static void PrintUsage()
{
    Console.WriteLine("""
        config-validator — validate config data-driven (schema + referential integrity + version).

        Cách dùng:
          config-validator <config-dir> [schema-dir]

        Tham số:
          <config-dir>   Thư mục config cần validate (vd: config)
          [schema-dir]   Thư mục JSON Schema (mặc định: shared/config-schema)

        Exit codes:
          0  hợp lệ
          1  có lỗi validate
          2  sai tham số / lỗi hạ tầng tool

        Ví dụ:
          config-validator config shared/config-schema
        """);
}
