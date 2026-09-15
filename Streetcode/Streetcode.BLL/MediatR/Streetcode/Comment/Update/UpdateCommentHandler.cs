using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.BLL.Interfaces.Logging;
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
            var errorMsg =
                $"Cannot find comment with id: {request.Id}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<CommentDto>(new Error(errorMsg));
        }

        if (comment.AuthorId != request.AuthorId)
        {
            var errorMsg =
                $"You do not have permission to update comment with id: {request.Id}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<CommentDto>(new Error(errorMsg));
        }

        comment.Text = request.Comment.Text.Trim();
        comment.UpdatedAt = DateTimeOffset.UtcNow;

        _repositoryWrapper.CommentRepository.Update(comment);
        bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;

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
