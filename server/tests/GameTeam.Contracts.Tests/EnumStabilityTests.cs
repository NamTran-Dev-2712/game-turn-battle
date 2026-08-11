using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using GameTeam.Contracts.Enums;
using Xunit;

namespace GameTeam.Contracts.Tests;

/// <summary>
/// Bảo vệ tính ỔN ĐỊNH của enum contract (docs/backend/api-and-versioning.md §4,
/// docs/roadmap/05-shared-contracts-skeleton.md). Test PHẢI đỏ nếu ai đó:
/// đổi số thứ tự, tái dùng số đã bỏ, đổi tên, hoặc xoá thành viên hiện có.
/// Thêm thành viên mới (additive) BẮT BUỘC cập nhật kỳ vọng ở đây một cách có chủ đích.
/// Khẳng định cả GIÁ TRỊ SỐ và CHUỖI canonical (đại diện serialize thực tế).
/// </summary>
public class EnumStabilityTests
{
    private static readonly JsonSerializerOptions StringEnumOptions =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static TheoryData<string, int> ExpectedMembers()
    {
        // Khoá cứng: (Type.Member) → giá trị số. Đầy đủ, đúng thứ tự khai báo.
        TheoryData<string, int> data = new();

        void Add(string name, int value) => data.Add(name, value);

        Add($"{nameof(Faction)}.{nameof(Faction.None)}", 0);

        Add($"{nameof(Class)}.{nameof(Class.None)}", 0);
        Add($"{nameof(Class)}.{nameof(Class.Warrior)}", 1);
        Add($"{nameof(Class)}.{nameof(Class.Mage)}", 2);
        Add($"{nameof(Class)}.{nameof(Class.Ranger)}", 3);
        Add($"{nameof(Class)}.{nameof(Class.Support)}", 4);

        Add($"{nameof(Element)}.{nameof(Element.None)}", 0);
        Add($"{nameof(Element)}.{nameof(Element.Fire)}", 1);
        Add($"{nameof(Element)}.{nameof(Element.Water)}", 2);
        Add($"{nameof(Element)}.{nameof(Element.Earth)}", 3);
        Add($"{nameof(Element)}.{nameof(Element.Light)}", 4);
        Add($"{nameof(Element)}.{nameof(Element.Dark)}", 5);

        Add($"{nameof(Role)}.{nameof(Role.None)}", 0);
        Add($"{nameof(Role)}.{nameof(Role.Tank)}", 1);
        Add($"{nameof(Role)}.{nameof(Role.Dps)}", 2);
        Add($"{nameof(Role)}.{nameof(Role.Support)}", 3);
        Add($"{nameof(Role)}.{nameof(Role.Healer)}", 4);

        Add($"{nameof(Rarity)}.{nameof(Rarity.None)}", 0);
        Add($"{nameof(Rarity)}.{nameof(Rarity.Three)}", 3);
        Add($"{nameof(Rarity)}.{nameof(Rarity.Four)}", 4);
        Add($"{nameof(Rarity)}.{nameof(Rarity.Five)}", 5);

        Add($"{nameof(Currency)}.{nameof(Currency.None)}", 0);
        Add($"{nameof(Currency)}.{nameof(Currency.Gold)}", 1);
        Add($"{nameof(Currency)}.{nameof(Currency.Gem)}", 2);
        Add($"{nameof(Currency)}.{nameof(Currency.Ticket)}", 3);

        return data;
    }

    [Theory]
    [MemberData(nameof(ExpectedMembers))]
    public void Enum_member_has_locked_numeric_value(string qualifiedName, int expectedValue)
    {
        string[] parts = qualifiedName.Split('.');
        Type enumType = ResolveEnum(parts[0]);
        object member = Enum.Parse(enumType, parts[1]);

        Convert.ToInt32(member).Should().Be(
            expectedValue,
            $"{qualifiedName} là contract ổn định — không đổi/không tái dùng số (api-and-versioning.md §4).");
    }

    [Fact]
    public void Enums_contain_exactly_the_locked_member_set()
    {
        // Bắt cả THÊM thành viên vô tình: tổng số thành viên phải khớp danh sách khoá cứng.
        int locked = ExpectedMembers().Count;
        int actual =
            Enum.GetNames<Faction>().Length
            + Enum.GetNames<Class>().Length
            + Enum.GetNames<Element>().Length
            + Enum.GetNames<Role>().Length
            + Enum.GetNames<Rarity>().Length
            + Enum.GetNames<Currency>().Length;

        actual.Should().Be(
            locked,
            "thay đổi tập thành viên enum phải là chủ đích + cập nhật kỳ vọng test (additive-only).");
    }

    [Fact]
    public void Enums_serialize_as_canonical_string_names()
    {
        JsonSerializer.Serialize(Role.Dps, StringEnumOptions).Should().Be("\"Dps\"");
        JsonSerializer.Serialize(Element.Fire, StringEnumOptions).Should().Be("\"Fire\"");
        JsonSerializer.Serialize(Currency.Gold, StringEnumOptions).Should().Be("\"Gold\"");
        JsonSerializer.Serialize(Rarity.Three, StringEnumOptions).Should().Be("\"Three\"");
        JsonSerializer.Serialize(Class.Warrior, StringEnumOptions).Should().Be("\"Warrior\"");
        JsonSerializer.Serialize(Faction.None, StringEnumOptions).Should().Be("\"None\"");

        // Round-trip chuỗi → enum.
        JsonSerializer.Deserialize<Role>("\"Healer\"", StringEnumOptions).Should().Be(Role.Healer);
    }

    private static Type ResolveEnum(string typeName) => typeName switch
    {
        nameof(Faction) => typeof(Faction),
        nameof(Class) => typeof(Class),
        nameof(Element) => typeof(Element),
        nameof(Role) => typeof(Role),
        nameof(Rarity) => typeof(Rarity),
        nameof(Currency) => typeof(Currency),
        _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Enum không thuộc contract."),
    };
}
