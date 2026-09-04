using FluentResults;
using MediatR;

namespace Streetcode.BLL.MediatR.Media.Video.Preview;

public record GetVideoForAdminPreviewCommand(string url) : IRequest<Result<string>>;