using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using MediatR;
namespace LTSBackend.Features.Users.Commands.DeleteUser;
public record DeleteUserCommand(int UserID): IRequest<bool>;