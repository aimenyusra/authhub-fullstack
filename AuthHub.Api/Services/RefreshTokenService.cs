using AuthHub.Api.Data;
using AuthHub.Api.Models;
using System.Security.Cryptography;

namespace AuthHub.Api.Services
{
    public class RefreshTokenService
    {
        private readonly AppDbContext _context;
        public RefreshTokenService(AppDbContext context)
        {
            _context = context;
        }
        public string GenerateToken ()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
        public string HashToken(string token)
        {
            
            byte[] tokenBytes = System.Text.Encoding.UTF8.GetBytes(token);
            byte[] hashBytes = SHA256.HashData(tokenBytes);
            return Convert.ToHexString(hashBytes);
        }
        public async Task<CreatedRefreshToken> CreateRefreshTokenAsync(Guid userId, Guid familyId)
        {
            string rawtoken = GenerateToken();
            string hashedToken = HashToken(rawtoken);
            var refreshToken = new RefreshToken
            {
                TokenHash = hashedToken,
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7), // Set expiration time as needed
                FamilyId = familyId
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
            return new CreatedRefreshToken(refreshToken, rawtoken);
        }
    }
    public record CreatedRefreshToken(RefreshToken Record, string RawToken);
}
