using FluentResults;
using MediatR;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;
using CommentEntity = Streetcode.DAL.Entities.Streetcode.Comment;

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
        cancellationToken.ThrowIfCancellationRequested();

        var comment = await _repositoryWrapper.CommentRepository.GetFirstOrDefaultAsync(
            predicate: comment => comment.Id == request.Id);

        if (comment is null)
        {
            var error = new CommentNotFoundError(request.Id);
            _logger.LogError(request, error.Message);
            return Result.Fail<Unit>(error);
        }

        var replies = await GetRepliesDepthFirstAsync(comment.Id, cancellationToken);
        if (replies.Count > 0)
        {
            _repositoryWrapper.CommentRepository.DeleteRange(replies);
        }

        _repositoryWrapper.CommentRepository.Delete(comment);

        cancellationToken.ThrowIfCancellationRequested();
        bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;
        if (!isSaved)
        {
            string errorMessage = $"Failed to delete comment with id: {request.Id}";
            _logger.LogError(request, errorMessage);
            return Result.Fail<Unit>(new Error(errorMessage));
        }

        return Result.Ok(Unit.Value);
    }

    private async Task<List<CommentEntity>> GetRepliesDepthFirstAsync(
        int commentId,
        CancellationToken cancellationToken)
    {
        var repliesDepthFirst = new List<CommentEntity>();
        var visitedIds = new HashSet<int> { commentId };
        var parentIds = new List<int> { commentId };

        while (parentIds.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var replies = (await _repositoryWrapper.CommentRepository.GetAllAsync(
                    reply => reply.ParentCommentId.HasValue &&
                             parentIds.Contains(reply.ParentCommentId.Value)))
                .Where(reply => visitedIds.Add(reply.Id))
                .ToList();

            if (replies.Count == 0)
            {
                break;
            }

            repliesDepthFirst.AddRange(replies);
            parentIds = replies.Select(reply => reply.Id).ToList();
        }

        repliesDepthFirst.Reverse();
        return repliesDepthFirst;
    }
}
