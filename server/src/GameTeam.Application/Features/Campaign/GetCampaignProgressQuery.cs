using GameTeam.Contracts.Campaign;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Campaign;

/// <summary>
/// Đọc tiến độ campaign của người gọi (server-authoritative, ADR-007) — trả <see cref="CampaignProgressDto"/>
/// đủ để client render (stage kèm mở/khoá/đã-clear + current AFK stage). Chủ sở hữu suy từ token (chống IDOR).
/// Chưa có tiến độ ⇒ trả trạng thái ban đầu (chỉ stage đầu mở khoá). Query thuần (không transaction).
/// </summary>
public sealed record GetCampaignProgressQuery : IRequest<Result<CampaignProgressDto>>;
