using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Text.GetParsed;

namespace Streetcode.BLL.MediatR.Streetcode.Text.Validators;

public sealed class GetParsedTextForAdminPreviewCommandValidator
    : AbstractValidator<GetParsedTextForAdminPreviewCommand>
{
    public GetParsedTextForAdminPreviewCommandValidator()
    {
        RuleFor(command => command.textToParse)
            .NotEmpty()
            .WithMessage("Text to parse cannot be empty.")
            .MaximumLength(15000)
            .WithMessage("Text to parse cannot exceed 15000 characters.");
    }
}