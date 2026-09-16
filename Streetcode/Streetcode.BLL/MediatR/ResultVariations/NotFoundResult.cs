using FluentResults;

namespace Streetcode.BLL.MediatR.ResultVariations;

public class NotFoundResult<T> : Result<T>
{
    public NotFoundResult(Error error)
    {
        WithError(error);
    }
}
