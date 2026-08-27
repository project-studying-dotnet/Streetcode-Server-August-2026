using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.DeleteSoft;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class DeleteSoftStreetcodeCommandValidator
    : AbstractValidator<DeleteSoftStreetcodeCommand>
{
    public DeleteSoftStreetcodeCommandValidator()
    {
        RuleFor(command => command.Id)
            .MustBeValidId("Streetcode");
    }
}