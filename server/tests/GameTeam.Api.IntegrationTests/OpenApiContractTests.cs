using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;

namespace GameTeam.Api.IntegrationTests;

/// <summary>
/// Cổng chất lượng OpenAPI (Phase 05): tài liệu phục vụ tại /openapi/v1.json phải HỢP LỆ
/// (không lỗi diagnostic của trình đọc OpenAPI) và mô tả đúng bề mặt contract nền + enum dùng chung.
/// Đây là "linter OpenAPI" chạy trong .NET toolchain (không cần Node).
/// </summary>
public class OpenApiContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OpenApiContractTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task<(OpenApiDocument Doc, OpenApiDiagnostic Diagnostic)> ReadDocumentAsync()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using Stream stream = await response.Content.ReadAsStreamAsync();
        OpenApiDocument doc = new OpenApiStreamReader().Read(stream, out OpenApiDiagnostic diagnostic);
        return (doc, diagnostic);
    }

    [Fact]
    public async Task OpenApi_document_is_valid()
    {
        (_, OpenApiDiagnostic diagnostic) = await ReadDocumentAsync();

        diagnostic.Errors.Should().BeEmpty("OpenAPI spec phải hợp lệ (không lỗi tham chiếu/schema).");
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/api/v1/auth/guest")]
    [InlineData("/api/v1/profile")]
    [InlineData("/api/v1/config/{version}")]
    public async Task OpenApi_document_exposes_foundation_paths(string path)
    {
        (OpenApiDocument doc, _) = await ReadDocumentAsync();

        doc.Paths.Should().ContainKey(path);
    }

    [Theory]
    [InlineData("AuthGuestRequest")]
    [InlineData("AuthGuestResponse")]
    [InlineData("ProfileDto")]
    [InlineData("ConfigBundleDto")]
    [InlineData("ConfigVersion")]
    [InlineData("ErrorResponse")]
    [InlineData("ErrorEnvelope")]
    [InlineData("HealthResponse")]
    public async Task OpenApi_document_exposes_foundation_dto_schemas(string schemaName)
    {
        (OpenApiDocument doc, _) = await ReadDocumentAsync();

        doc.Components.Schemas.Should().ContainKey(schemaName);
    }

    [Theory]
    [InlineData("Faction")]
    [InlineData("Class")]
    [InlineData("Element")]
    [InlineData("Role")]
    [InlineData("Rarity")]
    [InlineData("Currency")]
    public async Task OpenApi_document_exposes_shared_enums_as_strings(string enumName)
    {
        (OpenApiDocument doc, _) = await ReadDocumentAsync();

        doc.Components.Schemas.Should().ContainKey(enumName);
        OpenApiSchema schema = doc.Components.Schemas[enumName];
        schema.Type.Should().Be("string", $"{enumName} phải serialize dạng chuỗi (ổn định cho codegen).");
        schema.Enum.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("Rarity", new[] { 0, 3, 4, 5 })] // "khoảng trống" 1,2 — phải giữ đúng.
    [InlineData("Currency", new[] { 0, 1, 2, 3 })]
    [InlineData("Faction", new[] { 0 })]
    public async Task Shared_enums_carry_numeric_values_for_codegen(string enumName, int[] expected)
    {
        (OpenApiDocument doc, _) = await ReadDocumentAsync();

        OpenApiSchema schema = doc.Components.Schemas[enumName];
        // x-enum-values là NGUỒN để codegen client sinh enum GDScript khớp số của GameTeam.Contracts.
        schema.Extensions.Should().ContainKey("x-enum-values");
        var values = (OpenApiArray)schema.Extensions["x-enum-values"];
        values.Select(v => ((OpenApiInteger)v).Value).Should().Equal(expected);

        var varnames = (OpenApiArray)schema.Extensions["x-enum-varnames"];
        varnames.Should().HaveCount(expected.Length);
    }

    [Fact]
    public async Task Error_responses_reference_error_envelope()
    {
        (OpenApiDocument doc, _) = await ReadDocumentAsync();

        OpenApiResponse badRequest = doc.Paths["/api/v1/auth/guest"].Operations[OperationType.Post].Responses["400"];
        badRequest.Content["application/json"].Schema.Reference.Id.Should().Be("ErrorEnvelope");
    }
}
