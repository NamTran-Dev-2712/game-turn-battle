using System.Security.Cryptography;
using GameTeam.Application.Abstractions.Combat;

namespace GameTeam.Infrastructure.Combat;

/// <summary>
/// Hiện thực <see cref="IBattleSeedSource"/> bằng RNG mật mã (<see cref="RandomNumberGenerator"/>) — server là
/// bên sinh seed (ADR-011). Ép bit dấu = 0 ⇒ trả <b>Int64 không âm</b> để round-trip số nguyên an toàn ở client
/// (Godot <c>int</c> có dấu). Không dùng RNG global/wall-clock; seed đi tường minh vào sim (giữ tính tất định).
/// </summary>
public sealed class CryptoBattleSeedSource : IBattleSeedSource
{
    public long Next()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        long value = BitConverter.ToInt64(bytes);
        return value & long.MaxValue; // xoá bit dấu ⇒ [0, 2^63) — round-trip an toàn C# ↔ Godot int.
    }
}
