using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.GetById;

public class GetCommentByIdHandler : IRequestHandler<GetCommentByIdQuery, Result<CommentWithRepliesDto>>
{
    private readonly IMapper _mapper;
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly ILoggerService _logger;

    public GetCommentByIdHandler(
        IRepositoryWrapper repositoryWrapper,
        IMapper mapper,
        ILoggerService logger)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<CommentWithRepliesDto>> Handle(
        GetCommentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var comment = await _repositoryWrapper.CommentRepository.GetFirstOrDefaultAsync(
            predicate: comment => comment.Id == request.Id,
            include: query => query.Include(comment => comment.Replies));

        if (comment is null)
        {
            string errorMessage = $"Cannot find a comment with corresponding id: {request.Id}";
            _logger.LogError(request, errorMessage);
            return Result.Fail(new Error(errorMessage));
        }

        comment.Replies = comment.Replies
            .OrderBy(reply => reply.CreatedAt)
            .ThenBy(reply => reply.Id)
            .ToList();

        return Result.Ok(_mapper.Map<CommentWithRepliesDto>(comment));
    }
}
