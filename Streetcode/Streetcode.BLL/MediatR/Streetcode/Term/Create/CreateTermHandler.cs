using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.TextContent;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;
using TermEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Term;

namespace Streetcode.BLL.MediatR.Streetcode.Term.Create;

public class CreateTermHandler : IRequestHandler<CreateTermCommand, Result<TermDTO>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;
    private readonly ILoggerService _logger;

    public CreateTermHandler(IRepositoryWrapper repositoryWrapper, IMapper mapper, ILoggerService logger)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<TermDTO>> Handle(CreateTermCommand request, CancellationToken cancellationToken)
    {
        var term = _mapper.Map<TermEntity>(request.TermCreateDto);
        if (term is null)
        {
            const string errorMessage = "Term could not be created.";
            _logger.LogError(request, errorMessage);
            return Result.Fail(new Error(errorMessage));
        }

        var trimmedTitle = request.TermCreateDto.Title.Trim();
        var normalizedTitle = trimmedTitle.ToLower();
        var existingTerm = await _repositoryWrapper.TermRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Title != null && t.Title.ToLower() == normalizedTitle);

        if (existingTerm is not null)
        {
            string errorMessage = $"A term with the title '{request.TermCreateDto.Title}' already exists.";
            _logger.LogError(request, errorMessage);
            return Result.Fail<TermDTO>(errorMessage);
        }

        term.Title = trimmedTitle;
        var createdTerm = _repositoryWrapper.TermRepository.Create(term);
        var isSuccessResult = await _repositoryWrapper.SaveChangesAsync() > 0;
        if (!isSuccessResult)
        {
            const string errorMessage = "Cannot save changes in the database after creation";
            _logger.LogError(request, errorMessage);
            return Result.Fail<TermDTO>(errorMessage);
        }

        var createdTermDto = _mapper.Map<TermDTO>(createdTerm);
        if (createdTermDto != null)
        {
            return Result.Ok(createdTermDto);
        }

        const string errorMsg = "Cannot create term";
        _logger.LogError(request, errorMsg);
        return Result.Fail<TermDTO>(errorMsg);
    }
}
