namespace GameTeam.Domain.Common;

/// <summary>
/// Kết quả của một thao tác nghiệp vụ: thành công, hoặc thất bại kèm <see cref="Error"/>.
/// <para>
/// QUY ƯỚC (docs/backend/domain-and-application.md): dùng <c>Result</c> cho <b>lỗi nghiệp vụ mong đợi</b>
/// (thiếu tiền, không đủ điều kiện…). Lỗi lập trình / vi phạm bất biến nội bộ / lỗi hạ tầng vẫn <b>ném exception</b>.
/// Không biến mọi exception thành Result.
/// </para>
/// </summary>
public class Result
{
    /// <summary>
    /// Bất biến: thành công ⇔ <see cref="Error"/> = <see cref="Error.None"/>; thất bại ⇔ mang lỗi khác None.
    /// Vi phạm là lỗi lập trình ⇒ ném (không phải luồng nghiệp vụ).
    /// </summary>
    protected Result(bool isSuccess, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("Result thành công không được mang lỗi.", nameof(error));
        }

        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("Result thất bại phải mang lỗi khác Error.None.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Thao tác thành công.</summary>
    public bool IsSuccess { get; }

    /// <summary>Thao tác thất bại (nghịch đảo <see cref="IsSuccess"/>).</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Lỗi kèm theo; bằng <see cref="Error.None"/> khi thành công.</summary>
    public Error Error { get; }

    /// <summary>Tạo kết quả thành công không mang giá trị.</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>Tạo kết quả thất bại với <paramref name="error"/>.</summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Tạo <see cref="Result{TValue}"/> thành công mang <paramref name="value"/>.</summary>
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    /// <summary>Tạo <see cref="Result{TValue}"/> thất bại với <paramref name="error"/>.</summary>
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}
