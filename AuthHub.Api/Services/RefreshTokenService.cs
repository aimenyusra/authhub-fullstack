using AuthHub.Api.Data;
using AuthHub.Api.Models;
using Microsoft.EntityFrameworkCore;
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

        public async Task<RefreshRotationResult> RotateRefreshTokenAsync(string rawToken)
        {
            string tokenHash = HashToken(rawToken);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var existingToken = await _context.RefreshTokens
                                              .Include(token=> token.User)
                                              .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);
            if (existingToken == null)
            {
                return new RefreshRotationResult(false, true, null, null);
            }
            if (existingToken.RevokedAtUtc.HasValue)
            {
                var familyTokens = await _context.RefreshTokens
                                                  .Where(token =>
                                                  token.FamilyId == existingToken.FamilyId &&
                                                  token.RevokedAtUtc == null)
                                                  .ToListAsync();
                foreach (var token in familyTokens)
                {
                    token.RevokedAtUtc = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new RefreshRotationResult(false, true, null, null);
            }
            string newRawToken = GenerateToken();
            string newTokenHash = HashToken(newRawToken);
            existingToken.RevokedAtUtc = DateTime.UtcNow;
            existingToken.ReplacedByTokenHash = newTokenHash;
            var newRefreshToken = new RefreshToken
            {
                TokenHash = newTokenHash,
                UserId = existingToken.UserId,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7), // Set expiration time as needed
                FamilyId = existingToken.FamilyId
            };
            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new RefreshRotationResult(true, false, existingToken.User, newRawToken);

        }
    }
    public record RefreshRotationResult(
        bool Succes,
        bool ReuseDetected,
        User? User,
        string? RawToken);
    public record CreatedRefreshToken(RefreshToken Record, string RawToken);
}
