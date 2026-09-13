namespace GameTeam.Contracts.Summon;

/// <summary>
/// Kết quả MỘT lần quay (server-authoritative). Client chỉ HIỂN THỊ — không tự quyết. Hero mới ⇒
/// <see cref="IsNew"/> = true (đã vào sở hữu); trùng hero đã sở hữu ⇒ <see cref="IsNew"/> = false và
/// <see cref="Fragments"/> &gt; 0 (số mảnh đã cấp vào kho).
/// </summary>
/// <param name="HeroId">Hero trúng (id config <c>hero_*</c>).</param>
/// <param name="Rarity">Độ hiếm hero trúng (3/4/5).</param>
/// <param name="IsNew">Hero mới (đã cấp vào sở hữu) hay trùng (chuyển mảnh).</param>
/// <param name="Fragments">Số mảnh đã cấp nếu trùng (0 nếu là hero mới).</param>
public sealed record SummonPullDto(string HeroId, int Rarity, bool IsNew, long Fragments);
