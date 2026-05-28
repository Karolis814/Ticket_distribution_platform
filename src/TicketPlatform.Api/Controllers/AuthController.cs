using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Mail.Templates;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Services;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IUserService userService,
    IPasswordService passwordService,
    IPasswordResetTokenService passwordResetTokenService,
    IJWTService jwtService,
    IMailService mailService,
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
        var user = await userService.CreateAsync(dto, ct);

        var response = BuildAuthResponse(user);
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

        if (user is null)
            return Unauthorized();

        return Ok(new WhoAmIDTO(user.Id, user.Email));
    }

    [HttpPost("refresh")]
    [Authorize]
    public IActionResult Refresh()
    {
        var newToken = jwtService.RefreshAccessToken(User);
        var email    = User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "";
        Enum.TryParse<UserRole>(User.FindFirstValue("role"), out var role);

        return Ok(new AuthResponseDTO
        {
            AccessToken = newToken,
            ExpiresAt   = DateTime.UtcNow.AddMinutes(jwtService.AccessTokenExpiryMinutes),
            Email       = email,
            Role        = role
        });
    }

    [HttpPost("request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] ResetPasswordRequest req , CancellationToken ct)
    {
        
        var user = await userService.GetByEmailAsync(req.Email, ct);
        var userId = user.Id;
        var Token = PasswordResetTokenService.GenerateToken();
        var token = await passwordResetTokenService.CreateAsync(new PasswordResetToken
        {
            UserId = userId,
            TokenHash = PasswordResetTokenService.HashToken(Token),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        }, ct);
      
        string url = "";
        if (user != null)
        url = await passwordResetTokenService.CreatePasswordResetTokenAsync(user);
        await mailService.SendAsync(EmailTemplates.PasswordResetEmail(req.Email, Token, url), ct);
        return Ok();
    }


    [HttpPost("reset-password")]
    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest req)
{
    var user = await userService.GetByEmailAsync(req.Email);
        

    if (user is null)
        return false;

    var tokenHash = PasswordResetTokenService.HashToken(req.Token);

    var resetToken = await passwordResetTokenService.GetByUserId(user.Id);
       
    if (resetToken is null)
        return false;

    if (resetToken.ExpiresAt < DateTimeOffset.UtcNow)
        return false;

    resetToken.UsedAt = DateTimeOffset.UtcNow;
    var salt = passwordService.GenerateSalt();
    
    var hash = passwordService.HashPassword(req.NewPassword, salt );

    user.PasswordHash = hash;
    user.PasswordSalt = salt;

    await passwordResetTokenService.UpdateAsync(resetToken);
    await userService.UpdateAsync(user);

    return true;
}

    private AuthResponseDTO BuildAuthResponse(User user)
    {
        var accessToken = jwtService.GenerateAccessToken(user);

        return new AuthResponseDTO
        {
            AccessToken = accessToken,
            ExpiresAt   = DateTime.UtcNow.AddMinutes(jwtService.AccessTokenExpiryMinutes),
            Email       = user.Email,
            Role        = user.Role
        };
    }

}
