using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using GameTeam.Domain.Combat;
using GameTeam.Domain.Combat.Model;
using GameTeam.Domain.Combat.Serialization;

namespace GameTeam.CombatBaseline;

/// <summary>
/// Sinh + kiem tra <c>expected</c> baseline cua golden vector tu sim server (nguon chan ly, ADR-011).
/// <para>
/// <b>generate</b>: voi moi vector, doc <c>input</c> → <see cref="BattleSimulator.Simulate"/> →
/// <see cref="CombatEventSerializer"/> → ghi lai khoi <c>expected</c>; xuat CA FILE ve dang chuan tac
/// (2-space, LF, newline cuoi). Xac dinh + idempotent.
/// </para>
/// <para>
/// <b>check</b>: tao lai van ban chuan tac trong bo nho, so byte-for-byte voi file tren dia; lech =&gt; drift.
/// Day la co che chan "sua baseline am tham" — moi thay doi cong thuc phai regenerate co chu dich.
/// </para>
/// Tool nay KHONG fork sim: no goi thang <see cref="BattleSimulator"/> cua Domain (phase 24).
/// </summary>
public sealed class BaselineTool
{
    private static readonly JsonSerializerOptions CanonicalOptions = new()
    {
        WriteIndented = true,
        IndentCharacter = ' ',
        IndentSize = 2,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly string _vectorsDir;

    /// <summary>Tao tool doc/ghi vector trong thu muc chi dinh.</summary>
    public BaselineTool(string vectorsDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vectorsDir);
        _vectorsDir = vectorsDir;
    }

    /// <summary>Tao tool tro vao <c>shared/combat-vectors</c> cua repo.</summary>
    public static BaselineTool ForRepo() => new(RepoPaths.CombatVectorsDir);

    /// <summary>Liet ke moi file vector <c>*.json</c> (sap xep ordinal theo ten — xac dinh).</summary>
    public IReadOnlyList<string> DiscoverVectorFiles()
    {
        if (!Directory.Exists(_vectorsDir))
        {
            throw new DirectoryNotFoundException($"Khong thay thu muc vector: {_vectorsDir}");
        }

        return Directory.EnumerateFiles(_vectorsDir, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Dung van ban chuan tac cua ca file vector: giu <c>format_version/name/description/input</c>,
    /// tinh lai <c>expected</c> tu sim server. LF + newline cuoi.
    /// </summary>
    public static string BuildCanonicalText(string filePath)
    {
        string original = File.ReadAllText(filePath);
        JsonObject root = (JsonNode.Parse(original)
            ?? throw new InvalidDataException($"Vector rong/khong hop le: {filePath}")).AsObject();

        JsonNode inputNode = root["input"]
            ?? throw new InvalidDataException($"Vector thieu 'input': {filePath}");

        BattleInput input = VectorInputParser.Parse(inputNode);
        BattleOutput output = new BattleSimulator().Simulate(input);
        string expectedJson = CombatEventSerializer.Serialize(output);
        JsonNode expectedNode = JsonNode.Parse(expectedJson)!;

        root["expected"] = expectedNode;

        string text = root.ToJsonString(CanonicalOptions).Replace("\r\n", "\n", StringComparison.Ordinal);
        if (!text.EndsWith('\n'))
        {
            text += "\n";
        }

        return text;
    }

    /// <summary>Ghi baseline chuan tac cho cac file (mac dinh: tat ca). Tra ve ket qua tung file.</summary>
    public IReadOnlyList<VectorOutcome> Generate(IReadOnlyList<string>? files = null)
    {
        var results = new List<VectorOutcome>();
        foreach (string path in Resolve(files))
        {
            string canonical = BuildCanonicalText(path);
            string current = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal) : string.Empty;
            bool changed = !string.Equals(canonical, current, StringComparison.Ordinal);
            if (changed)
            {
                File.WriteAllText(path, canonical, Utf8NoBom);
            }

            results.Add(new VectorOutcome(Path.GetFileName(path), changed ? VectorStatus.Written : VectorStatus.Unchanged));
        }

        return results;
    }

    /// <summary>
    /// So baseline tren dia voi ban tao lai tu sim server. Tra ve tung file: khop hay drift.
    /// KHONG ghi file.
    /// </summary>
    public IReadOnlyList<VectorOutcome> Check(IReadOnlyList<string>? files = null)
    {
        var results = new List<VectorOutcome>();
        foreach (string path in Resolve(files))
        {
            string canonical = BuildCanonicalText(path);
            string current = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
            bool ok = string.Equals(canonical, current, StringComparison.Ordinal);
            results.Add(new VectorOutcome(Path.GetFileName(path), ok ? VectorStatus.Match : VectorStatus.Drift));
        }

        return results;
    }

    private IReadOnlyList<string> Resolve(IReadOnlyList<string>? files)
    {
        if (files is null || files.Count == 0)
        {
            return DiscoverVectorFiles();
        }

        return files
            .Select(f => Path.IsPathRooted(f) ? f : Path.Combine(_vectorsDir, f))
            .ToList();
    }
}

/// <summary>Ket qua xu ly mot vector.</summary>
public sealed record VectorOutcome(string FileName, VectorStatus Status);

/// <summary>Trang thai mot vector sau generate/check.</summary>
public enum VectorStatus
{
    /// <summary>generate: file da khop, khong ghi lai.</summary>
    Unchanged,

    /// <summary>generate: da ghi baseline moi/chuan hoa.</summary>
    Written,

    /// <summary>check: baseline tren dia khop sim server.</summary>
    Match,

    /// <summary>check: baseline tren dia LECH sim server (drift).</summary>
    Drift,
}
