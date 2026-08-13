namespace GameTeam.Domain.Common;

/// <summary>
/// <see cref="Result"/> mang giá trị <typeparamref name="TValue"/> khi thành công.
/// Truy cập <see cref="Value"/> lúc thất bại là trạng thái không hợp lệ ⇒ ném (lỗi lập trình).
/// </summary>
/// <typeparam name="TValue">Kiểu giá trị trả về khi thành công.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
        => _value = value;

    /// <summary>
    /// Giá trị khi thành công. Ném <see cref="InvalidOperationException"/> nếu truy cập khi thất bại.
    /// </summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Không thể truy cập Value của Result thất bại.");

    /// <summary>Ép ngầm <see cref="Error"/> → <see cref="Result{TValue}"/> thất bại cho tiện dùng.</summary>
    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
