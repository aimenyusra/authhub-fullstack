using AuthHub.Api.Data;
using AuthHub.Api.DTOs;
using AuthHub.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthHub.Api.Services
{
    public class RegistrationService
    {
        private readonly AppDbContext _context;

        public RegistrationService(AppDbContext context)
        {
            _context = context;
        }   
        public async Task<bool> EmailExistsAsync (string email)
        {
            string normalizedEmail = email.Trim().ToLowerInvariant();
            return await _context.Users.AnyAsync(u => u.Email == normalizedEmail);
        }  
        public async Task<User> RegisterAsync (RegistrationRequestDto request)
        {
            string normalizedEmail = request.Email.Trim().ToLowerInvariant();
            bool emailExists = await EmailExistsAsync(normalizedEmail);
            if (emailExists)
            {
                throw new InvalidOperationException("Email already exists.");
            }
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                PasswordHash = passwordHash,
                Role = "User" ,// Default role
                IsEmailVerified = false,
                CreatedAtUtc = DateTime.UtcNow,

            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
        public async Task<User?> LoginAsync(LoginRequestDto request)
        {
            string normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            if (user is null)
            {
                return null;
            }
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return null;
            }
            return user;
        }
    }
}
