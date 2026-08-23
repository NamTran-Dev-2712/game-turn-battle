using GameTeam.Contracts.Profile;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Profile.Queries;

/// <summary>
/// Pure read of the authenticated caller's profile (owner resolved from the token <c>sub</c>). Returns
/// <c>PROFILE_NOT_FOUND</c> if none exists — it never creates one (that is
/// <see cref="Commands.GetOrCreateProfileCommand"/>). Not transactional, not cacheable (player state is
/// server truth and per-account; see ADR-007).
/// </summary>
public sealed record GetMyProfileQuery : IRequest<Result<ProfileDto>>;
