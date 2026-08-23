using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Streetcode.BLL.DTO.Users;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Entities.Users;

namespace Streetcode.BLL.MediatR.Users.Register;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, Result<RegisterUserDTO>>
{
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    private readonly ILoggerService _logger;

    public RegisterUserHandler(UserManager<User> userManager, IMapper mapper, ILoggerService logger)
    {
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<RegisterUserDTO>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var user = _mapper.Map<User>(request.RegisterUserDTO);

        var result = await _userManager.CreateAsync(user, request.RegisterUserDTO.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError(request, errors);
            return Result.Fail(new Error(errors));
        }

        if (!string.IsNullOrEmpty(request.RegisterUserDTO.Role))
        {
            var roleResult = await _userManager.AddToRoleAsync(user, request.RegisterUserDTO.Role);
            if (!roleResult.Succeeded)
            {
                var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                _logger.LogError(request, $"Failed to add role: {roleErrors}");
                return Result.Fail(new Error(roleErrors));
            }
        }

        return Result.Ok(request.RegisterUserDTO);
    }
}