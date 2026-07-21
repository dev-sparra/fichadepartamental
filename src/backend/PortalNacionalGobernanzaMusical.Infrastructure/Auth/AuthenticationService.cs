using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PortalNacionalGobernanzaMusical.Application.Auth;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Auth;

public sealed class AuthenticationService(
    ApplicationDbContext dbContext,
    IOptions<JwtSettings> jwtOptions) : IAuthenticationService
{
    private readonly PasswordHasher<UserAccount> passwordHasher = new();

    public async Task<AuthenticatedUserResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await dbContext.UserAccounts
            .Include(x => x.RoleAssignments)
            .ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail && x.IsActive, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var jwtSettings = jwtOptions.Value;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(jwtSettings.AccessTokenMinutes);
        var roles = user.RoleAssignments.Select(x => x.Role?.Name ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.Email)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtSettings.Issuer,
            Audience = jwtSettings.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAtUtc,
            SigningCredentials = credentials
        };

        return new AuthenticatedUserResult(
            new JsonWebTokenHandler().CreateToken(tokenDescriptor),
            expiresAtUtc,
            user.Email,
            user.DisplayName,
            roles,
            user.MustChangePassword);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new ArgumentException("La nueva contraseña debe tener al menos 8 caracteres.");

        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
            throw new ArgumentException("La nueva contraseña debe ser diferente a la actual.");

        var user = await dbContext.UserAccounts.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
            if (verification == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("La contraseña actual es incorrecta.");
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}