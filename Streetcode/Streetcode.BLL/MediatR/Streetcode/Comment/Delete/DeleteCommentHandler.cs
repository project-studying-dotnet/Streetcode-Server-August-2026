using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Delete;

public class DeleteCommentHandler : IRequestHandler<DeleteCommentCommand, Result<Unit>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly ILoggerService _logger;

    public DeleteCommentHandler(
        IRepositoryWrapper repositoryWrapper,
        ILoggerService logger)
    {
        _repositoryWrapper = repositoryWrapper;
        _logger = logger;
    }

    public async Task<Result<Unit>> Handle(
        DeleteCommentCommand request,
        CancellationToken cancellationToken)
    {
        var comment = await _repositoryWrapper.CommentRepository.GetFirstOrDefaultAsync(
            predicate: comment => comment.Id == request.Id,
            include: query => query.Include(comment => comment.Replies));

        if (comment is null)
        {
            string errorMessage = $"Cannot find a comment with corresponding id: {request.Id}";
            _logger.LogError(request, errorMessage);
            return Result.Fail<Unit>(new Error(errorMessage));
        }

        _repositoryWrapper.CommentRepository.DeleteRange(comment.Replies);
        _repositoryWrapper.CommentRepository.Delete(comment);

        bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;
        if (!isSaved)
        {
            string errorMessage = $"Failed to delete comment with id: {request.Id}";
            _logger.LogError(request, errorMessage);
            return Result.Fail<Unit>(new Error(errorMessage));
        }

        return Result.Ok(Unit.Value);
    }
}
