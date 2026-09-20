using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Streetcode.WebApi.Identity;

namespace Streetcode.WebApi.Controllers.Users;

public class RegistrationController : BaseApiController
{
    private readonly UserManager<RegistrationUser> _userManager;
    private readonly IRegistrationTransactionFactory _transactionFactory;

    public RegistrationController(
        UserManager<RegistrationUser> userManager,
        IRegistrationTransactionFactory transactionFactory)
    {
        _userManager = userManager;
        _transactionFactory = transactionFactory;
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Surname))
        {
            return BadRequest("Name and surname are required.");
        }

        var email = request.Email.Trim();
        var user = new RegistrationUser
        {
            UserName = email,
            Email = email,
            Name = request.Name.Trim(),
            Surname = request.Surname.Trim(),
        };

        await using var transaction = await _transactionFactory.BeginAsync();
        try
        {
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors.Select(error => error.Description));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "User");
            if (!roleResult.Succeeded)
            {
                return StatusCode(500, "Could not assign the user role.");
            }

            await transaction.CommitAsync();
            return Created($"/api/registration/{user.Id}", new { user.Id, user.Email });
        }
        catch (Exception)
        {
            return StatusCode(500, "Could not create the user account.");
        }
    }
}
