namespace GameTeam.Contracts.Summon;

/// <summary>
/// Yêu cầu triệu hồi (gacha) — <b>chỉ là INTENT</b> của client (server-authoritative, ADR-011): client gửi
/// banner + số lần quay + khoá idempotency; <b>server</b> quyết RNG/rate/pity/hero/thưởng. Chủ sở hữu suy từ
/// token (không nhận trong body — chống IDOR).
/// </summary>
/// <param name="BannerId">Id banner (config <c>gacha_*</c>).</param>
/// <param name="Count">Số lần quay: <c>1</c> (đơn) hoặc <c>10</c> (mười) — server kiểm.</param>
/// <param name="RequestId">Khoá idempotency do client sinh: retry cùng khoá ⇒ trả kết quả đã lưu, KHÔNG quay lại.</param>
public sealed record SummonRequest(string BannerId, int Count, string RequestId);
