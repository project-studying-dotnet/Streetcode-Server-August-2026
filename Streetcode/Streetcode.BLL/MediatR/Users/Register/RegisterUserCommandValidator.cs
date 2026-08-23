using FluentValidation;

namespace Streetcode.BLL.MediatR.Users.Register;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.RegisterUserDTO.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(command => command.RegisterUserDTO.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");

        RuleFor(command => command.RegisterUserDTO.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(50).WithMessage("Name cannot exceed 50 characters.");

        RuleFor(command => command.RegisterUserDTO.Surname)
            .NotEmpty().WithMessage("Surname is required.")
            .MaximumLength(50).WithMessage("Surname cannot exceed 50 characters.");
    }
}