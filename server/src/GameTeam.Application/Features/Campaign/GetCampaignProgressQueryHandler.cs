using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Contracts.Campaign;
using GameTeam.Domain.Campaign;
using GameTeam.Domain.Common;
using MediatR;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;

namespace GameTeam.Application.Features.Campaign;

/// <summary>
/// Handles <see cref="GetCampaignProgressQuery"/>: chủ sở hữu từ token → profile → tiến độ đã lưu (có thể chưa
/// có) → dựng <see cref="CampaignProgressDto"/> từ chuỗi chapter + tập cleared (mở/khoá/đã-clear + current AFK
/// stage do server tính — <see cref="CampaignMapping.ToDto"/>). Không mutation.
/// </summary>
public sealed class GetCampaignProgressQueryHandler
    : IRequestHandler<GetCampaignProgressQuery, Result<CampaignProgressDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPlayerProfileRepository _profiles;
    private readonly ICampaignProgressRepository _progressRepo;
    private readonly CampaignChain _chain;

    public GetCampaignProgressQueryHandler(
        ICurrentUser currentUser,
        IPlayerProfileRepository profiles,
        ICampaignProgressRepository progressRepo,
        CampaignChain chain)
    {
        _currentUser = currentUser;
        _profiles = profiles;
        _progressRepo = progressRepo;
        _chain = chain;
    }

    public async Task<Result<CampaignProgressDto>> Handle(
        GetCampaignProgressQuery request, CancellationToken cancellationToken)
    {
        Guid? accountId = _currentUser.AccountId;
        if (accountId is null)
        {
            return Result.Failure<CampaignProgressDto>(CampaignErrors.Unauthenticated);
        }

        DomainProfile? profile = await _profiles.GetByAccountIdAsync(accountId.Value, cancellationToken);
        if (profile is null)
        {
            return Result.Failure<CampaignProgressDto>(CampaignErrors.ProfileNotFound);
        }

        CampaignProgress? progress = await _progressRepo.GetByProfileIdAsync(profile.Id, cancellationToken);
        return Result.Success(CampaignMapping.ToDto(_chain, progress));
    }
}
