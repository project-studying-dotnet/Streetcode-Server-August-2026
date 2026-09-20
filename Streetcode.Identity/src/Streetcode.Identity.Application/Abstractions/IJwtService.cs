using System;
using System.Collections.Generic;
using System.Text;

namespace Streetcode.Identity.Application.Abstractions
{
    public interface IJwtService
    {
        AuthTokenResult GenerateToken(Guid userId, string email, IEnumerable<string> roles, long accessVersion);
    }
}
