using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services
{
    public class PasswordService
    {
        private readonly PasswordHasher<User> _hasher = new();

        public string HashPassword(User user, string plainPassword)
        {
           return _hasher.HashPassword(user, plainPassword);
        }

        public bool VerifyPassword(User user, string hashedPassword, string plainPassword)
        { 
            var verifypass = _hasher.VerifyHashedPassword(user, hashedPassword, plainPassword);
            return verifypass == PasswordVerificationResult.Success;
        }
    }
}