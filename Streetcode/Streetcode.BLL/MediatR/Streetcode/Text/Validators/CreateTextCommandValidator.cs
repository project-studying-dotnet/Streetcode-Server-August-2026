using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.Text.Create;
using TextEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Text;

namespace Streetcode.BLL.MediatR.Streetcode.Text.Validators;
public sealed class CreateTextCommandValidator : AbstractValidator<CreateTextCommand>
{
    public CreateTextCommandValidator()
    {
        RuleFor(command => command.TextCreateDto.TextContent)
            .NotEmpty()
            .WithMessage("Text content is required.")
            .MustNotExceedLength(
                TextEntity.TextContentMaxLength,
                "Text content");

        RuleFor(command => command.TextCreateDto.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MustNotExceedLength(
                TextEntity.TitleMaxLength,
                "Title");

        RuleFor(command => command.TextCreateDto.AdditionalText)
            .MustNotExceedLength(
                TextEntity.AdditionalTextMaxLength,
                "Additional text");

        RuleFor(command => command.TextCreateDto.StreetcodeId)
            .MustBeValidId("Streetcode");
    }
}
