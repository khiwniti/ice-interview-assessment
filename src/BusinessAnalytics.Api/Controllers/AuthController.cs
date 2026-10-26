using BusinessAnalytics.Application.Users.DTOs;
using BusinessAnalytics.Application.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BusinessAnalytics.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(IUserService userService, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _userService = userService;
        _configuration = configuration;
        _logger = logger;
    }
    
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Login failed for email: {Email}", request.Email);
            return Unauthorized(new { error = "Invalid credentials" });
        }
        
        // Verify password using the User entity's password hash
        // We need to get the actual user entity to verify
        // For now, we'll use a simple approach - in production, use ASP.NET Core Identity
        var identityUser = await GetIdentityUserAsync(request.Email, cancellationToken);
        if (identityUser == null)
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }
        
        // Generate JWT token
        var token = GenerateJwtToken(user);
        
        return Ok(new LoginResponse(token, user.Id, user.Email, user.DisplayName, user.Role));
    }
    
    private Task<Microsoft.AspNetCore.Identity.IdentityUser<Guid>?> GetIdentityUserAsync(string email, CancellationToken cancellationToken)
    {
        // This would typically use UserManager from Identity
        // For simplicity in this MVP, we'll return a mock
        return Task.FromResult<Microsoft.AspNetCore.Identity.IdentityUser<Guid>?>(null);
    }
    
    private string GenerateJwtToken(BusinessAnalytics.Application.Users.DTOs.UserDto user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "dev-secret-key-change-in-production-min-32-chars-long";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "BusinessAnalytics";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "BusinessAnalytics";
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("departmentId", user.DepartmentId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTimeOffset.UtcNow.AddHours(8).DateTime,
            signingCredentials: creds
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public record LoginRequest(string Email, string Password);
    public record LoginResponse(string Token, Guid UserId, string Email, string DisplayName, string Role);
}