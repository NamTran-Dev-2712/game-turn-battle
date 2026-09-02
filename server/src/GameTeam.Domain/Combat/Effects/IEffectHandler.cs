namespace GameTeam.Domain.Combat.Effects;

/// <summary>
/// Handler thực thi <b>một</b> loại effect (skill-framework.md, ADR-004). Đăng ký theo
/// <see cref="EffectType"/> trong <see cref="EffectRegistry"/>. Mở rộng gameplay = thêm handler mới +
/// config — <b>không</b> sửa lõi combat (OCP). Handler phải <b>tất định</b> (không wall-clock/RNG global).
/// </summary>
public interface IEffectHandler
{
    /// <summary>Khoá định tuyến (ví dụ <c>damage</c>, <c>heal</c>) — khớp <c>effect_type</c> trong config.</summary>
    string EffectType { get; }

    /// <summary>Áp effect lên trạng thái trận qua <paramref name="context"/> (đọc dữ liệu + phát sự kiện).</summary>
    void Apply(EffectContext context);
}
