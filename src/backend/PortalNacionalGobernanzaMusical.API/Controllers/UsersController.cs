using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalNacionalGobernanzaMusical.Application.Administration;

namespace PortalNacionalGobernanzaMusical.API.Controllers;

[ApiController]
[Authorize]
[Route("api/administration/[controller]")]
public sealed class UsersController(IUserAdministrationService userService) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyCollection<UserDto>>> GetAllAsync(CancellationToken ct) => Ok(await userService.GetUsersAsync(ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<UserDto>> GetAsync(Guid id, CancellationToken ct) { var u = await userService.GetUserAsync(id, ct); return u is null ? NotFound() : Ok(u); }
    [HttpPost] public async Task<ActionResult<UserDto>> CreateAsync([FromBody] CreateUserRequest r, CancellationToken ct) => Ok(await userService.CreateUserAsync(r, ct));
    [HttpPut("{id:guid}")] public async Task<ActionResult<UserDto>> UpdateAsync(Guid id, [FromBody] UpdateUserRequest r, CancellationToken ct) => Ok(await userService.UpdateUserAsync(id, r, ct));

    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult<ResetPasswordResult>> ResetPasswordAsync(Guid id, CancellationToken ct)
    {
        var result = await userService.ResetUserPasswordAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
