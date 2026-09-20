using Microsoft.AspNetCore.Identity;

namespace Streetcode.WebApi.Identity;

public class RegistrationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;
}
