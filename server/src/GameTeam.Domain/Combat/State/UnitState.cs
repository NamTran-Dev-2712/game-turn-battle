using GameTeam.Domain.Combat.Model;

namespace GameTeam.Domain.Combat.State;

/// <summary>
/// Trạng thái <b>khả biến</b> của một đơn vị trong lúc mô phỏng (HP/energy/buff/hồi chiêu). Danh tính =
/// <see cref="ActorId"/> (từ snapshot). Không phụ thuộc thứ tự dictionary/hash — mọi thứ tự lấy từ khoá
/// sort tường minh của simulator (§13/§14/§23). <see cref="Atk"/>/<see cref="Def"/>/<see cref="Spd"/> là
/// chỉ số <b>hiệu dụng</b> = nền + tổng modifier (buff/debuff), kẹp ≥ 0.
/// </summary>
public sealed class UnitState
{
    private readonly List<StatModifier> _modifiers = new();

    /// <summary>Ảnh chụp bất biến gốc (chỉ số cơ bản).</summary>
    public UnitSnapshot Snapshot { get; }

    /// <summary>Định danh ổn định, duy nhất trong trận.</summary>
    public string ActorId => Snapshot.ActorId;

    /// <summary>Đội (<c>ally</c>/<c>enemy</c>).</summary>
    public string Team => Snapshot.Team;

    /// <summary>Vị trí đội hình 0..5.</summary>
    public int Slot => Snapshot.Slot;

    /// <summary>Bộ skill riêng của đơn vị (§23) — <c>null</c> ⇒ dùng basic skill dùng chung của trận.</summary>
    public UnitSkillSet? Skills => Snapshot.Skills;

    /// <summary>Tốc độ hiệu dụng (nền + modifier) — quyết thứ tự lượt.</summary>
    public int Spd => Effective(Snapshot.Stats.Spd, StatKind.Spd);

    /// <summary>Tấn công hiệu dụng (nền + modifier).</summary>
    public int Atk => Effective(Snapshot.Stats.Atk, StatKind.Atk);

    /// <summary>Phòng thủ hiệu dụng (nền + modifier).</summary>
    public int Def => Effective(Snapshot.Stats.Def, StatKind.Def);

    /// <summary>HP tối đa (HP đầu trận).</summary>
    public int MaxHp => Snapshot.Stats.Hp;

    /// <summary>HP hiện thời (≥ 0).</summary>
    public int Hp { get; private set; }

    /// <summary>Năng lượng hiện thời (§15).</summary>
    public int Energy { get; private set; }

    /// <summary>Số vòng còn hồi chiêu ultimate (§15).</summary>
    public int UltimateCooldownRemaining { get; private set; }

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

    /// <summary>Cộng <paramref name="amount"/> năng lượng (kẹp <c>[0, max]</c>). Trả <c>true</c> nếu giá trị đổi.</summary>
    public bool AddEnergy(int amount, int max)
    {
        int before = Energy;
        long next = (long)Energy + amount;
        if (next < 0)
        {
            next = 0;
        }

        if (next > max)
        {
            next = max;
        }

        Energy = (int)next;
        return Energy != before;
    }

    /// <summary>Tiêu <paramref name="amount"/> năng lượng (kẹp ≥ 0). Trả <c>true</c> nếu giá trị đổi.</summary>
    public bool SpendEnergy(int amount)
    {
        int before = Energy;
        int next = Energy - amount;
        Energy = next < 0 ? 0 : next;
        return Energy != before;
    }

    /// <summary>Đặt hồi chiêu ultimate (số vòng, kẹp ≥ 0).</summary>
    public void SetUltimateCooldown(int rounds) => UltimateCooldownRemaining = rounds < 0 ? 0 : rounds;

    /// <summary>Giảm hồi chiêu ultimate 1 vòng (không xuống dưới 0).</summary>
    public void TickUltimateCooldown()
    {
        if (UltimateCooldownRemaining > 0)
        {
            UltimateCooldownRemaining--;
        }
    }

    /// <summary>
    /// Áp/refresh một modifier chỉ số. Khoá = (<paramref name="sourceSkillId"/>, <paramref name="stat"/>):
    /// nếu đã có ⇒ thay <paramref name="signedAmount"/> + <paramref name="durationRounds"/> (không chồng).
    /// </summary>
    public void ApplyStatModifier(string sourceSkillId, StatKind stat, int signedAmount, int durationRounds)
    {
        foreach (StatModifier existing in _modifiers)
        {
            if (existing.Stat == stat && string.Equals(existing.SourceSkillId, sourceSkillId, StringComparison.Ordinal))
            {
                existing.Amount = signedAmount;
                existing.RemainingRounds = durationRounds;
                return;
            }
        }

        _modifiers.Add(new StatModifier(sourceSkillId, stat, signedAmount, durationRounds));
    }

    /// <summary>
    /// Giảm 1 vòng mọi modifier; gỡ những cái hết hạn. Trả danh sách <b>đã gỡ</b> theo thứ tự tất định
    /// (stat: atk,def,spd; rồi source_skill_id ordinal) để phát <c>BuffExpired</c>.
    /// </summary>
    public IReadOnlyList<StatModifier> TickModifiers()
    {
        var expired = new List<StatModifier>();
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            _modifiers[i].RemainingRounds -= 1;
            if (_modifiers[i].RemainingRounds <= 0)
            {
                expired.Add(_modifiers[i]);
                _modifiers.RemoveAt(i);
            }
        }

        expired.Sort(static (a, b) =>
        {
            int byStat = a.Stat.CompareTo(b.Stat);
            return byStat != 0 ? byStat : string.CompareOrdinal(a.SourceSkillId, b.SourceSkillId);
        });
        return expired;
    }

    private int Effective(int baseValue, StatKind stat)
    {
        int sum = 0;
        foreach (StatModifier modifier in _modifiers)
        {
            if (modifier.Stat == stat)
            {
                sum += modifier.Amount;
            }
        }

        int value = baseValue + sum;
        return value < 0 ? 0 : value;
    }
}
