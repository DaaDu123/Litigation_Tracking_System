using MediatR;
namespace LTSBackend.Features.LoginHistory.Commands.DeleteOldHistory;

public record DeleteOldHistoryCommand(int Days) : IRequest<int>;