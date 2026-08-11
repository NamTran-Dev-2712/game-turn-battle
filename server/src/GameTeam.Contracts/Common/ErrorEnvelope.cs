namespace GameTeam.Contracts.Common;

/// <summary>
/// Vỏ bọc lỗi trên dây (wire): body lỗi là <c>{ "error": { code, message, traceId } }</c>
/// (docs/backend/api-and-versioning.md §3). Đây là kiểu trả về cho mọi response lỗi.
/// </summary>
/// <param name="Error">Nội dung lỗi.</param>
public sealed record ErrorEnvelope(ErrorResponse Error);
