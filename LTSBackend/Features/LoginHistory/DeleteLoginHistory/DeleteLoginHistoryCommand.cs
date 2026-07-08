using MediatR;
namespace LTSBackend.Features.LoginHistory.Commands.DeleteLoginHistory;
public record DeleteLoginHistoryCommand(int LoginID): IRequest<bool>;