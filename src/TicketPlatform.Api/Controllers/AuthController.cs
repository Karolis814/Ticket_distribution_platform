using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Services;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IUserService userService,
    IPasswordService passwordService,
    IJWTService jwtService,
    IRepository<User> userRepository) : ControllerBase
{
    
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] UserSignUpDTO dto,
        CancellationToken ct)
    {
        var emailTaken = await userRepository.Query()
            .AnyAsync(u => u.Email == dto.Email, ct);

        if (emailTaken)
            return Conflict(new { message = "Email is already in use." });


        // return new JsonResult(dto);
        User user;
        try
        {
            user = await userService.CreateAsync(dto, ct);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message }); 
        }

        // need to call from service        
        var userWithPerms = await userRepository.Query()
            .Include(u => u.UserPermissionGroup)
                .ThenInclude(g => g.Permissions)
            .FirstAsync(u => u.Id == user.Id, ct);

        var response = BuildAuthResponse(userWithPerms);
        return CreatedAtAction(nameof(Register), response);
    }

    // POST api/auth/login
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] UserLoginDTO dto,
        CancellationToken ct)
    {
        var user = await userRepository.Query()
            .Include(u => u.UserPermissionGroup)
                .ThenInclude(g => g.Permissions)
            .FirstOrDefaultAsync(u => u.Email == dto.Email, ct);

    
        if (user is null || !passwordService.VerifyPassword(dto.Password, user.PasswordSalt, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var response = BuildAuthResponse(user);
        return Ok(response);
    }
    // which get user id  
    [HttpGet("me")]
    [ProducesResponseType(typeof(WhoAmIDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
     public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        if (userId is null)
            return Unauthorized();

        var user = await userService.GetByIdAsync(Guid.Parse(userId), ct);

        return Ok(new WhoAmIDTO(
            user.Id,
            user.Email ?? string.Empty
        ));
    }

    private AuthResponseDTO BuildAuthResponse(User user)
    {
        var accessToken = jwtService.GenerateAccessToken(user);

        return new AuthResponseDTO
        {
            AccessToken     = accessToken,
            ExpiresAt       = DateTime.UtcNow.AddMinutes(15), // mirrors JwtSettings value
            Email           = user.Email,
            Username        = user.Username,
            PermissionGroup = user.UserPermissionGroup.Title
        };
    }

}