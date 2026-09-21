using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Comment.Delete;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Update;

public class UpdateCommentHandler : IRequestHandler<UpdateCommentCommand, Result<CommentDto>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;
    private readonly ILoggerService _loggerService;

    public UpdateCommentHandler(IRepositoryWrapper repositoryWrapper, IMapper mapper, ILoggerService loggerService)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _loggerService = loggerService;
    }

    public async Task<Result<CommentDto>> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _repositoryWrapper.CommentRepository
            .GetFirstOrDefaultAsync(
                predicate: comment => comment.Id == request.Id);

        if (comment is null)
        {
            var error = new CommentNotFoundError(request.Id);
            _loggerService.LogError(request, error.Message);
            return Result.Fail<CommentDto>(error);
        }

        if (comment.AuthorId != request.AuthorId)
        {
            var error = new CommentForbiddenError(request.Id);
            _loggerService.LogError(request, error.Message);
            return Result.Fail<CommentDto>(error);
        }

        if (!request.Comment.RowVersion.SequenceEqual(comment.RowVersion))
        {
            var error = new CommentConflictError(request.Id);
            _loggerService.LogError(request, error.Message);
            return Result.Fail<CommentDto>(error);
        }

        comment.Text = request.Comment.Text.Trim();
        comment.UpdatedAt = DateTimeOffset.UtcNow;

        _repositoryWrapper.CommentRepository.Update(comment);
        bool isSaved;
        try
        {
            isSaved = await _repositoryWrapper.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (DbUpdateConcurrencyException)
        {
            var error = new CommentConflictError(request.Id);
            _loggerService.LogError(request, error.Message);
            return Result.Fail<CommentDto>(error);
        }

        if (!isSaved)
        {
            var errorMsg =
                $"Failed to update comment with id: {request.Id}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<CommentDto>(new Error(errorMsg));
        }

        var commentDto = _mapper.Map<CommentDto>(comment);
        return Result.Ok(commentDto);
    }
}
