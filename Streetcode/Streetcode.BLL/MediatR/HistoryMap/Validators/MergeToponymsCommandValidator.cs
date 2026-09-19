using FluentValidation;
using Streetcode.BLL.DTO.HistoryMap;
using Streetcode.BLL.MediatR.HistoryMap.Merge;

namespace Streetcode.BLL.MediatR.HistoryMap.Validators
{
    public sealed class MergeToponymsCommandValidator
        : AbstractValidator<MergeToponymsCommand>
    {
        public MergeToponymsCommandValidator(
            IValidator<MergeToponymsDTO> dtoValidator)
        {
            RuleFor(command => command.Dto)
                .NotNull()
                .WithMessage("Merge toponyms DTO is required.")
                .SetValidator(dtoValidator);
        }
    }
}