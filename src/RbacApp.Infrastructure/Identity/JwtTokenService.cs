using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Application.Common.Models;
using RbacApp.Domain.Entities.Identity;
using RbacApp.Domain.Exceptions;

namespace RbacApp.Infrastructure.Identity;

/// <summary>
/// تولید و چرخش توکن‌های JWT و refresh.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantDbContextFactory _dbFactory;
    private readonly ILogger<JwtTokenService> _logger;

    private readonly TimeSpan _accessTokenLifetime;
    private readonly TimeSpan _refreshTokenLifetime;
    private readonly SigningCredentials _signingCredentials;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenService(
        IConfiguration config,
        ITenantContext tenantContext,
        ITenantDbContextFactory dbFactory,
        ILogger<JwtTokenService> logger)
    {
        _config = config;
        _tenantContext = tenantContext;
        _dbFactory = dbFactory;
        _logger = logger;

        var key = config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key در پیکربندی تعریف نشده است.");
        _issuer = config["Jwt:Issuer"] ?? "RbacApp";
        _audience = config["Jwt:Audience"] ?? "RbacApp";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        _accessTokenLifetime = TimeSpan.FromMinutes(
            double.Parse(config["Jwt:AccessTokenMinutes"] ?? "15"));
        _refreshTokenLifetime = TimeSpan.FromDays(
            double.Parse(config["Jwt:RefreshTokenDays"] ?? "7"));
    }

    public async Task<AuthTokenDto> GenerateForUserAsync(
        Guid userId, string email, string fullName,
        IEnumerable<string> roles, IEnumerable<string> permissions,
        string? ip, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(_accessTokenLifetime);

        var claims = BuildClaims(userId, email, fullName, roles, permissions, now);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: _signingCredentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshTokenStr = GenerateRefreshTokenString();

        // ذخیره‌ی هش refresh token در دیتابیس tenant.
        await using var db = _dbFactory.CreateForCurrentTenant();
        await db.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(refreshTokenStr),
            ExpiresAt = expires.Add(_refreshTokenLifetime).DateTime,
            CreatedByIp = ip
        }, ct);
        await db.SaveChangesAsync(ct);

        return new AuthTokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenStr,
            ExpiresAt = expires,
            UserId = userId,
            Email = email,
            FullName = fullName,
            Roles = roles.ToList().AsReadOnly(),
            Permissions = permissions.ToList().AsReadOnly()
        };
    }

    public async Task<AuthTokenDto> RotateRefreshTokenAsync(
        string refreshToken, string? ip, CancellationToken ct = default)
    {
        // جستجوی توکن از روی هش.
        var tokenHash = HashToken(refreshToken);

        // نیاز به DbContext داریم — tenant رو از ITenantContext می‌گیریم.
        if (!_tenantContext.IsAvailable)
            throw new AppException("tenant جاری مشخص نیست.", "no_tenant");

        await using var db = _dbFactory.CreateForCurrentTenant();
        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (existing is null || existing.IsExpired || existing.IsRevoked)
            throw new UnauthorizedException("refresh token نامعتبر یا منقضی است.");

        // باطل کردن توکن قبلی.
        existing.RevokedAt = DateTime.UtcNow;
        existing.RevokedReason = "rotated";
        existing.ReplacedByTokenHash = tokenHash; // موقتاً، بعد جایگزین می‌شود.

        var user = await db.Users.FindAsync(existing.UserId, ct)
            ?? throw new NotFoundException("User", existing.UserId);

        // تولید توکن جدید.
        var roles = await db.Context
            .Set<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>()
            .Where(ur => ur.UserId == user.Id)
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name!)
            .ToListAsync(ct);

        var permissionIds = await db.Context
            .Set<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>()
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        var permissions = await db.RolePermissions
            .Where(rp => permissionIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission)
            .Distinct()
            .ToListAsync(ct);

        // تولید refresh token جدید.
        var newRefreshStr = GenerateRefreshTokenString();
        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(_accessTokenLifetime);

        var newEntry = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(newRefreshStr),
            ExpiresAt = expires.Add(_refreshTokenLifetime).DateTime,
            CreatedByIp = ip
        };
        existing.ReplacedByTokenHash = newEntry.TokenHash;

        db.RefreshTokens.Add(newEntry);
        await db.SaveChangesAsync(ct);

        var claims = BuildClaims(user.Id, user.Email!, user.FullName, roles, permissions, now);
        var jwt = new JwtSecurityToken(
            issuer: _issuer, audience: _audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: _signingCredentials);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new AuthTokenDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshStr,
            ExpiresAt = expires,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            Roles = roles,
            Permissions = permissions
        };
    }

    public async Task RevokeUserTokensAsync(Guid userId, string reason, CancellationToken ct = default)
    {
        if (!_tenantContext.IsAvailable) return;

        await using var db = _dbFactory.CreateForCurrentTenant();
        var tokens = await db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = reason;
        }

        await db.SaveChangesAsync(ct);
    }

    public Task<JwtPrincipal?> GetPrincipalFromExpiredTokenAsync(string token)
    {
        try
        {
            var key = _config["Jwt:Key"]!;
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // توکن منقضی هم قبول می‌شود.
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out _);

            if (principal.FindFirstValue(JwtRegisteredClaimNames.Sub) is not string sub)
                return Task.FromResult<JwtPrincipal?>(null);

            if (!Guid.TryParse(sub, out var userId))
                return Task.FromResult<JwtPrincipal?>(null);

            var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "";
            var fullName = principal.FindFirstValue("fullName") ?? "";
            var tenantId = principal.FindFirstValue("tenantId") ?? "";
            var tenantSlug = principal.FindFirstValue("tenantSlug") ?? "";
            var roles = principal.FindAll("role").Select(c => c.Value).ToList();

            var result = Guid.TryParse(tenantId, out var tenantGuid)
                ? new JwtPrincipal(userId, email, tenantGuid, tenantSlug, roles)
                : new JwtPrincipal(userId, email, Guid.Empty, tenantSlug, roles);

            return Task.FromResult<JwtPrincipal?>(result);
        }
        catch
        {
            return Task.FromResult<JwtPrincipal?>(null);
        }
    }

    private static List<Claim> BuildClaims(
        Guid userId, string email, string fullName,
        IEnumerable<string> roles, IEnumerable<string> permissions,
        DateTimeOffset now)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString()),
            new("fullName", fullName),
            new("tenantId", ""),
            new("tenantSlug", "")
        };

        foreach (var role in roles)
            claims.Add(new Claim("role", role));

        foreach (var perm in permissions)
            claims.Add(new Claim("permission", perm));

        return claims;
    }

    private static string GenerateRefreshTokenString()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
