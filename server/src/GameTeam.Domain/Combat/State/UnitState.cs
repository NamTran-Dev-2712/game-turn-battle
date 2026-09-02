using GameTeam.Domain.Combat.Model;

namespace GameTeam.Domain.Combat.State;

/// <summary>
/// Trạng thái <b>khả biến</b> của một đơn vị trong lúc mô phỏng (HP/energy hiện thời). Danh tính =
/// <see cref="ActorId"/> (từ snapshot). Không phụ thuộc thứ tự dictionary/hash — mọi thứ tự lấy từ
/// khoá sort tường minh của simulator (§13/§14).
/// </summary>
public sealed class UnitState
{
    /// <summary>Ảnh chụp bất biến gốc (chỉ số cơ bản).</summary>
    public UnitSnapshot Snapshot { get; }

    /// <summary>Định danh ổn định, duy nhất trong trận.</summary>
    public string ActorId => Snapshot.ActorId;

    /// <summary>Đội (<c>ally</c>/<c>enemy</c>).</summary>
    public string Team => Snapshot.Team;

    /// <summary>Vị trí đội hình 0..5.</summary>
    public int Slot => Snapshot.Slot;

    /// <summary>Tốc độ (quyết thứ tự lượt).</summary>
    public int Spd => Snapshot.Stats.Spd;

    /// <summary>Tấn công.</summary>
    public int Atk => Snapshot.Stats.Atk;

    /// <summary>Phòng thủ.</summary>
    public int Def => Snapshot.Stats.Def;

    /// <summary>HP tối đa (HP đầu trận).</summary>
    public int MaxHp => Snapshot.Stats.Hp;

    /// <summary>HP hiện thời (≥ 0).</summary>
    public int Hp { get; private set; }

    /// <summary>Năng lượng hiện thời (§15 — cơ chế đề xuất, chưa kích hoạt ở phase 24).</summary>
    public int Energy { get; private set; }

    /// <summary>Đơn vị còn sống?</summary>
    public bool IsAlive => Hp > 0;

    /// <summary>Khởi tạo từ snapshot + năng lượng ban đầu.</summary>
    public UnitState(UnitSnapshot snapshot, int initialEnergy)
    {
        Snapshot = snapshot;
        Hp = snapshot.Stats.Hp;
        Energy = initialEnergy < 0 ? 0 : initialEnergy;
    }

    /// <summary>Trừ <paramref name="amount"/> HP (kẹp về 0). Trả HP còn lại.</summary>
    public int ApplyDamage(int amount)
    {
        Hp = amount >= Hp ? 0 : Hp - amount;
        return Hp;
    }

    /// <summary>Hồi <paramref name="amount"/> HP (kẹp về <see cref="MaxHp"/>). Trả HP còn lại.</summary>
    public int Heal(int amount)
    {
        int next = Hp + amount;
        Hp = next > MaxHp ? MaxHp : next;
        return Hp;
    }
}
