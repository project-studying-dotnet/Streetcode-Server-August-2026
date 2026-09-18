using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Comment.Delete;
using Streetcode.DAL.Repositories.Interfaces.Base;
using CommentEntity = Streetcode.DAL.Entities.Streetcode.Comment;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Reply;

public class CreateReplyHandler : IRequestHandler<CreateReplyCommand, Result<CommentDto>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;
    private readonly ILoggerService _loggerService;

    public CreateReplyHandler(
        IRepositoryWrapper repositoryWrapper,
        IMapper mapper,
        ILoggerService loggerService)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _loggerService = loggerService;
    }

    public async Task<Result<CommentDto>> Handle(CreateReplyCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var parentComment = await _repositoryWrapper.CommentRepository.GetFirstOrDefaultAsync(
            predicate: comment => comment.Id == request.ParentCommentId);

        if (parentComment is null)
        {
            var error = new CommentNotFoundError(request.ParentCommentId);
            _loggerService.LogError(request, error.Message);
            return Result.Fail<CommentDto>(error);
        }

        if (parentComment.ParentCommentId.HasValue)
        {
            var errorMessage =
                $"Cannot reply to comment with id: {request.ParentCommentId} because it is already a reply.";
            _loggerService.LogError(request, errorMessage);
            return Result.Fail<CommentDto>(new Error(errorMessage));
        }

        var reply = new CommentEntity
        {
            StreetcodeId = parentComment.StreetcodeId,
            ParentCommentId = parentComment.Id,
            AuthorId = request.AuthorId,
            Text = request.Reply.Text.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await _repositoryWrapper.CommentRepository.CreateAsync(reply);

        cancellationToken.ThrowIfCancellationRequested();
        bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!isSaved)
        {
            var errorMessage = $"Failed to create reply for comment with id: {request.ParentCommentId}";
            _loggerService.LogError(request, errorMessage);
            return Result.Fail<CommentDto>(new Error(errorMessage));
        }

        return Result.Ok(_mapper.Map<CommentDto>(reply));
    }
}
