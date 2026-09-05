using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Contracts.Hero;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Heroes.Queries;

/// <summary>
/// Handles <see cref="GetHeroDefinitionQuery"/>: đọc <see cref="HeroConfig"/> từ bundle hiện hành qua
/// <see cref="IConfigProvider"/> (KHÔNG chạm filesystem) → map <see cref="HeroDefinitionDto"/>. Thiếu ⇒
/// <c>HERO_DEFINITION_NOT_FOUND</c> (không đoán giá trị mặc định — ADR-004).
/// </summary>
public sealed class GetHeroDefinitionQueryHandler
    : IRequestHandler<GetHeroDefinitionQuery, Result<HeroDefinitionDto>>
{
    private readonly IConfigProvider _config;

    public GetHeroDefinitionQueryHandler(IConfigProvider config) => _config = config;

    public Task<Result<HeroDefinitionDto>> Handle(
        GetHeroDefinitionQuery request,
        CancellationToken cancellationToken)
    {
        HeroConfig? config = _config.Get<HeroConfig>(HeroMapping.ConfigType, request.HeroId);

        Result<HeroDefinitionDto> result = config is null
            ? HeroErrors.DefinitionNotFound
            : Result.Success(HeroMapping.ToDefinitionDto(request.HeroId, config));

        return Task.FromResult(result);
    }
}
