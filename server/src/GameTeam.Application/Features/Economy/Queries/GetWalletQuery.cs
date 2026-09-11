using GameTeam.Contracts.Economy;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Economy.Queries;

/// <summary>
/// Đọc số dư ví của người gọi (chủ sở hữu suy từ token <c>sub</c>). Trả ví <b>rỗng</b> (chưa có dòng số dư)
/// nếu chưa có ví — không phải lỗi. Read-only: KHÔNG tạo/sửa, không transactional, không cacheable (state
/// người chơi là chân lý server, per-account — ADR-007).
/// </summary>
public sealed record GetWalletQuery : IRequest<Result<WalletDto>>;
