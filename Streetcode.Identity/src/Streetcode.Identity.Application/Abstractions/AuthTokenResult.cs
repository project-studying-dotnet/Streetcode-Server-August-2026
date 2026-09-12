using System;
using System.Collections.Generic;
using System.Text;

namespace Streetcode.Identity.Application.Abstractions
{
    public record AuthTokenResult(string Token, DateTime Expiration);
}
