using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

        var trimmedTitle = request.TermCreateDto.Title.Trim();
        var existingTerm = await _repositoryWrapper.TermRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Title == trimmedTitle);

        if (existingTerm is not null)
        {
            string errorMessage = $"A term with the title '{trimmedTitle}' already exists.";
            _logger.LogError(request, errorMessage);
            return Result.Fail<TermDTO>(errorMessage);
        }

        term.Title = trimmedTitle;
        term.Description = request.TermCreateDto.Description.Trim();
        var createdTerm = _repositoryWrapper.TermRepository.Create(term);
        try
        {
            var isSuccessResult = await _repositoryWrapper.SaveChangesAsync() > 0;
            if (!isSuccessResult)
            {
                const string errorMessage = "Cannot save changes in the database after creation";
                _logger.LogError(request, errorMessage);
                return Result.Fail<TermDTO>(errorMessage);
            }
        }
        catch (DbUpdateException)
        {
            string errorMessage = $"A term with the title '{trimmedTitle}' already exists.";
            _logger.LogError(request, errorMessage);
            return Result.Fail<TermDTO>(errorMessage);
        }

        var createdTermDto = _mapper.Map<TermDTO>(createdTerm);
        return Result.Ok(createdTermDto);
    }
}
