using LTSBackend.Features.Profile.DTOs;
using MediatR;
namespace LTSBackend.Features.Profile.Queries;
public record GetMyProfileQuery(int UserID) : IRequest<ProfileDTO>;