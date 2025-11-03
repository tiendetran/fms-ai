using Dapper;
using FAS.Api.Data;
using FAS.Core.DTOs;
using FAS.Core.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FAS.Api.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IDatabaseContext _dbContext;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IConfiguration configuration,
        IDatabaseContext dbContext,
        ILogger<AuthService> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string?> AuthenticateAsync(string username, string password)
    {
        try
        {
            using var connection = _dbContext.CreatePostgreSqlConnection();

            // Query user từ database (giả sử có bảng users)
            var query = @"
                SELECT user_id, username, password_hash, full_name, role
                FROM users 
                WHERE username = @Username AND is_active = true
                LIMIT 1";

            var user = await connection.QueryFirstOrDefaultAsync<dynamic>(
                query,
                new { Username = username });

            if (user == null)
            {
                _logger.LogWarning("User not found: {Username}", username);
                return null;
            }

            // Verify password (trong thực tế nên dùng BCrypt hoặc tương tự)
            if (!VerifyPassword(password, user.password_hash))
            {
                _logger.LogWarning("Invalid password for user: {Username}", username);
                return null;
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);
            _logger.LogInformation("User authenticated successfully: {Username}", username);

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating user: {Username}", username);
            throw;
        }
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new InvalidOperationException());

            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public string GenerateJwtToken(dynamic user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new InvalidOperationException());

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.user_id.ToString()),
            new Claim(ClaimTypes.Name, user.username),
            new Claim(ClaimTypes.GivenName, user.full_name ?? string.Empty),
            new Claim(ClaimTypes.Role, user.role ?? "User"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(secretKey);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = int.Parse(jwtSettings["ExpiryInMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private bool VerifyPassword(string password, string passwordHash)
    {
        // Trong production, sử dụng BCrypt.Net-Next hoặc tương tự
        // Ví dụ: return BCrypt.Net.BCrypt.Verify(password, passwordHash);

        // Đơn giản hóa cho demo - KHÔNG dùng trong production
        return password == passwordHash; // Cần thay bằng proper hashing
    }

    public Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        throw new NotImplementedException();
    }
}
