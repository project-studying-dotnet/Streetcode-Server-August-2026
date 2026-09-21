using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.Text.GetParsed;
using Streetcode.BLL.Resources;
using TextEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Text;

namespace Streetcode.BLL.MediatR.Streetcode.Text.Validators;

public sealed class GetParsedTextForAdminPreviewCommandValidator
    : AbstractValidator<GetParsedTextForAdminPreviewCommand>
{
    public GetParsedTextForAdminPreviewCommandValidator()
    {
        RuleFor(command => command.textToParse)
            .NotEmpty()
            .WithName("Text to parse")
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(
                TextEntity.TextContentMaxLength,
                "Text to parse");
    }
}