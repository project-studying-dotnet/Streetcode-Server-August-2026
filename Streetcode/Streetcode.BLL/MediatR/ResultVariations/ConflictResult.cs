using FluentResults;

namespace Streetcode.BLL.MediatR.ResultVariations;

public class ConflictResult<T> : Result<T>
{
    public ConflictResult(Error error)
    {
        WithError(error);
    }
}
