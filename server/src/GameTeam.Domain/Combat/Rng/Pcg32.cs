namespace GameTeam.Domain.Combat.Rng;

/// <summary>
/// PRNG <b>PCG32</b> (variant <c>pcg_setseq_64_xsh_rr_32</c>) + seed-expander <b>SplitMix64</b>
/// (combat-framework.md §11, ADR-011). Một <see cref="Pcg32"/> = <b>một stream/trận</b>, seed là một
/// <see cref="ulong"/> server-generated truyền tường minh — <b>KHÔNG</b> RNG global/ambient, KHÔNG
/// <c>System.Random</c>, KHÔNG timestamp/OS randomness. State là bit-pattern <b>unsigned 64-bit</b>;
/// mọi phép nhân <b>wrap mod 2^64</b> (<c>unchecked</c>), mọi dịch phải là <b>logical shift</b>.
/// Mọi hằng/độ rộng khớp bit-for-bit với client (GDScript, phase 25) để golden vector trùng khít.
/// </summary>
public sealed class Pcg32
{
    private const ulong PcgMult = 6364136223846793005UL; // 0x5851F42D4C957F2D

    private ulong _state;
    private readonly ulong _inc;

    /// <summary>Khởi tạo stream từ <paramref name="seed"/> (uint64) qua SplitMix64 → (initstate, initseq) → PCG seed.</summary>
    public Pcg32(ulong seed)
    {
        unchecked
        {
            ulong sm = seed;
            (sm, ulong initState) = SplitMix64Next(sm);
            (_, ulong initSeq) = SplitMix64Next(sm);

            _inc = (initSeq << 1) | 1UL; // phải lẻ
            _state = 0UL;
            _state = (_state * PcgMult) + _inc; // step
            _state += initState;
            _state = (_state * PcgMult) + _inc; // step
        }
    }

    /// <summary>Một bước SplitMix64: trả (state mới, output 64-bit).</summary>
    private static (ulong State, ulong Output) SplitMix64Next(ulong state)
    {
        unchecked
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (state, z);
        }
    }

    /// <summary>Sinh 32-bit kế tiếp (advance state, xuất qua xorshift + rotate — logical shift).</summary>
    public uint NextUInt32()
    {
        unchecked
        {
            ulong old = _state;
            _state = (old * PcgMult) + _inc;
            uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
        }
    }

    /// <summary>
    /// Sinh số không thiên vị trong [0, <paramref name="bound"/>) bằng rejection-sampling (§11).
    /// Vòng lặp không có trần thử lại (theo spec — không tồn tại hằng cap). <paramref name="bound"/> ≥ 1.
    /// </summary>
    public uint Bounded(uint bound)
    {
        if (bound == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bound), bound, "bound phải ≥ 1.");
        }

        unchecked
        {
            uint threshold = (uint)((0x1_0000_0000UL - bound) % bound);
            while (true)
            {
                uint r = NextUInt32();
                if (r >= threshold)
                {
                    return r % bound;
                }
            }
        }
    }
}
