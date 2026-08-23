using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Users;

namespace Streetcode.BLL.MediatR.Users.Register;

public record RegisterUserCommand(RegisterUserDTO RegisterUserDTO) : IRequest<Result<RegisterUserDTO>>;