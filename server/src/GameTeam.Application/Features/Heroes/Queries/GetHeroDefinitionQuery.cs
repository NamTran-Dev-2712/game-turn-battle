using GameTeam.Contracts.Hero;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Heroes.Queries;

/// <summary>
/// Đọc định nghĩa tĩnh của một hero từ config hiện hành qua <c>IConfigProvider</c> (data-driven, ADR-004) —
/// chứng minh server đọc definition từ config, đổi config ⇒ đổi dữ liệu KHÔNG sửa code. Không tồn tại ⇒
/// <c>HERO_DEFINITION_NOT_FOUND</c>. Nội dung config chung, không nhạy cảm (endpoint public).
/// </summary>
/// <param name="HeroId">Id definition hero (prefix <c>hero_</c>).</param>
public sealed record GetHeroDefinitionQuery(string HeroId) : IRequest<Result<HeroDefinitionDto>>;
