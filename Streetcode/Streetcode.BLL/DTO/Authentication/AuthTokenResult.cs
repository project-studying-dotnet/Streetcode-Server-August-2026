using System;
using System.Collections.Generic;
using System.Text;

namespace Streetcode.BLL.DTO.Authentication
{
    public record AuthTokenResult(string Token, DateTime Expiration);
}
