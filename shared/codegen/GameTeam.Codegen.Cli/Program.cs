using GameTeam.Codegen;

// CLI MỎNG: parse args → gọi core → in report → exit code.
// KHÔNG chứa logic sinh mã (nằm trong GameTeam.Codegen — ranh giới tái dùng).
//
// Exit codes:
//   0 = sinh xong (idempotent)
//   1 = (dành cho tương lai) khác biệt cần chú ý — hiện không dùng
//   2 = lỗi sử dụng hoặc hạ tầng tool (sai tham số, thiếu OpenAPI, contract chưa hỗ trợ)

const int ExitOk = 0;
const int ExitUsageOrToolError = 2;

const string DefaultOpenApi = "shared/contracts/openapi.json";
const string DefaultOutput = "client/src/data/generated";

string[] positional = args.Where(a => !a.StartsWith('-')).ToArray();

if (args.Contains("-h") || args.Contains("--help"))
{
    PrintUsage();
    return ExitOk;
}

if (positional.Length > 2)
{
    Console.Error.WriteLine("Lỗi: tối đa 2 tham số vị trí: [openapi-path] [output-dir].");
    PrintUsage();
    return ExitUsageOrToolError;
}

string openApiPath = positional.Length >= 1 ? positional[0] : DefaultOpenApi;
string outputDir = positional.Length == 2 ? positional[1] : DefaultOutput;

try
{
    CodegenReport report = CodegenRunner.Run(new CodegenOptions(openApiPath, outputDir));

    Console.WriteLine($"OK: sinh {report.Written.Count} file GDScript vào {outputDir}"
        + (report.Deleted.Count > 0 ? $" (xoá {report.Deleted.Count} file stale)" : "")
        + $" từ {openApiPath}.");
    foreach (string file in report.Written)
    {
        Console.WriteLine($"  + {file}");
    }

    foreach (string file in report.Deleted)
    {
        Console.WriteLine($"  - {file} (stale)");
    }

    return ExitOk;
}
catch (CodegenException ex)
{
    // Contract chứa cấu trúc chưa hỗ trợ — chỉ rõ schema:property:reason.
    Console.Error.WriteLine($"Lỗi codegen (contract chưa hỗ trợ): {ex.Message}");
    Console.Error.WriteLine("Bảng kiểu hỗ trợ + giới hạn: shared/codegen/README.md.");
    return ExitUsageOrToolError;
}
catch (Exception ex) when (ex is InvalidOperationException or IOException or System.Text.Json.JsonException)
{
    Console.Error.WriteLine($"Lỗi tool: {ex.Message}");
    return ExitUsageOrToolError;
}

static void PrintUsage()
{
    Console.WriteLine("""
        codegen — sinh model client GDScript từ hợp đồng OpenAPI (Phase 08).

        Cách dùng:
          codegen [openapi-path] [output-dir]

        Tham số:
          [openapi-path]  Đường dẫn openapi.json (mặc định: shared/contracts/openapi.json)
          [output-dir]    Thư mục đầu ra GDScript (mặc định: client/src/data/generated)

        Exit codes:
          0  sinh xong
          2  sai tham số / lỗi hạ tầng / contract chưa hỗ trợ

        Ví dụ:
          codegen shared/contracts/openapi.json client/src/data/generated
        """);
}
