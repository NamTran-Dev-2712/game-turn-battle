namespace GameTeam.Contracts.Common;

/// <summary>
/// Body của endpoint liveness <c>GET /health</c> (hạ tầng, không phải API game).
/// Kiểu hoá để mô tả được trong OpenAPI; giữ nguyên hình dạng <c>{ "status": "ok" }</c>.
/// </summary>
/// <param name="Status">Trạng thái host (vd "ok").</param>
public sealed record HealthResponse(string Status);
