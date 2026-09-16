using FluentResults;

namespace Streetcode.BLL.MediatR.ResultVariations;

public class ForbiddenResult<T> : Result<T>
{
    public ForbiddenResult(Error error)
    {
        WithError(error);
    }
}
