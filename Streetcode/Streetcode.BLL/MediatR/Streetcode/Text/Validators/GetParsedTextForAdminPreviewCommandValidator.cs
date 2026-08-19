using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.Text.GetParsed;
using TextEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Text;

namespace Streetcode.BLL.MediatR.Streetcode.Text.Validators;

public sealed class GetParsedTextForAdminPreviewCommandValidator
    : AbstractValidator<GetParsedTextForAdminPreviewCommand>
{
    public GetParsedTextForAdminPreviewCommandValidator()
    {
        RuleFor(command => command.textToParse)
            .NotEmpty()
            .WithMessage("Text to parse is required.")
            .MustNotExceedLength(
                TextEntity.TextContentMaxLength,
                "Text to parse");
    }
}