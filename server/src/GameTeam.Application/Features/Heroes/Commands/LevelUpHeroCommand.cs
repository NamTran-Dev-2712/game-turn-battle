using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Contracts.Hero;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Heroes.Commands;

/// <summary>
/// Nâng cấp một hero lên MỘT cấp — server-authoritative (ADR-004/007/011): server lấy cấp hiện tại + đường
/// cong config → tính chi phí → <b>tiêu gold atomic</b> (Phase 31) → tăng cấp → tính lại chỉ số + Power. Tất
/// cả trong <b>một transaction</b> (<see cref="ITransactionalRequest"/> ⇒ TransactionBehavior): thiếu tiền hay
/// bất kỳ lỗi nào ⇒ KHÔNG trừ tiền, KHÔNG tăng cấp (rollback). Chủ sở hữu suy từ token (không nhận trong
/// command — chống IDOR). Idempotency của lần tiêu = khoá theo (profile, hero, cấp đích) + khoá dòng hero.
/// </summary>
/// <param name="HeroId">Id definition hero ở config (<c>hero_*</c>) mà người chơi muốn nâng.</param>
public sealed record LevelUpHeroCommand(string HeroId)
    : IRequest<Result<LevelUpHeroResponse>>, ITransactionalRequest;
