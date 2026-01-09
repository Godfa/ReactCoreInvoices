using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Services;
using Application.Interfaces;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Persistence;

namespace API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DataContext _context;
        private readonly IHostEnvironment _environment;
        private readonly IEmailService _emailService;

        public AdminController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, DataContext context, IHostEnvironment environment, IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _environment = environment;
            _emailService = emailService;
        }

        [HttpGet("users")]
        public async Task<ActionResult<List<UserManagementDto>>> GetUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = users.Select(u => new UserManagementDto
            {
                Id = u.Id,
                UserName = u.UserName,
                DisplayName = u.DisplayName,
                Email = u.Email,
                MustChangePassword = u.MustChangePassword
            }).ToList();

            return userDtos;
        }

        [HttpPost("users")]
        public async Task<ActionResult<UserManagementDto>> CreateUser(CreateUserDto createUserDto)
        {
            if (await _userManager.Users.AnyAsync(x => x.Email == createUserDto.Email))
            {
                return BadRequest("Email already exists");
            }

            if (await _userManager.Users.AnyAsync(x => x.UserName == createUserDto.UserName))
            {
                return BadRequest("Username already exists");
            }

            // Same email check handled above

            var user = new User
            {
                UserName = createUserDto.UserName,
                DisplayName = createUserDto.DisplayName,
                Email = createUserDto.Email,
                MustChangePassword = true
            };

            // Generate a temporary password
            var tempPassword = "TempPass123!";
            var result = await _userManager.CreateAsync(user, tempPassword);

            if (!result.Succeeded)
            {
                return BadRequest("Failed to create user");
            }

            // Send email with credentials
            var emailSent = await _emailService.SendNewUserEmailAsync(
                user.Email,
                user.DisplayName,
                user.UserName,
                tempPassword
            );

            return new UserManagementDto
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                Email = user.Email,
                MustChangePassword = user.MustChangePassword
            };
        }

        [HttpPut("users/{id}")]
        public async Task<ActionResult> UpdateUser(string id, UpdateUserDto updateUserDto)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Check if email is taken by another user
            if (await _userManager.Users.AnyAsync(x => x.Email == updateUserDto.Email && x.Id != id))
            {
                return BadRequest("Email already exists");
            }

            user.DisplayName = updateUserDto.DisplayName;
            user.Email = updateUserDto.Email;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest("Failed to update user");
            }

            return Ok();
        }

        [HttpPost("users/{id}/reset-password")]
        public async Task<ActionResult<string>> SendPasswordResetLink(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Generate a new random temporary password
            var newPassword = GenerateRandomPassword();

            // Remove old password and set new one
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!resetResult.Succeeded)
            {
                return BadRequest("Failed to reset password");
            }

            // Set MustChangePassword flag
            user.MustChangePassword = true;
            await _userManager.UpdateAsync(user);

            // Send password reset email with new temporary password
            var emailSent = await _emailService.SendPasswordResetEmailAsync(
                user.Email,
                user.DisplayName,
                newPassword
            );

            var message = emailSent
                ? $"Password reset email sent to {user.Email} with new temporary password."
                : $"Password reset but email could not be sent to {user.Email}. New temporary password: {newPassword}";

            return Ok(new { message });
        }

        private string GenerateRandomPassword()
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";
            const string all = uppercase + lowercase + digits + special;

            var random = new Random();
            var password = new char[12];

            // Ensure at least one of each required type
            password[0] = uppercase[random.Next(uppercase.Length)];
            password[1] = lowercase[random.Next(lowercase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = special[random.Next(special.Length)];

            // Fill the rest randomly
            for (int i = 4; i < 12; i++)
            {
                password[i] = all[random.Next(all.Length)];
            }

            // Shuffle the password
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }

        [HttpPost("reseed-database")]
        public async Task<ActionResult> ReseedDatabase()
        {
            try
            {
                // Clear existing invoices
                var existingInvoices = await _context.Invoices
                    .Include(i => i.ExpenseItems)
                        .ThenInclude(ei => ei.Payers)
                    .Include(i => i.Participants)
                    .ToListAsync();

                _context.Invoices.RemoveRange(existingInvoices);
                await _context.SaveChangesAsync();

                // Run seed data
                await Seed.SeedData(_context, _userManager, _roleManager, _environment);

                return Ok(new { message = "Database reseeded successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Failed to reseed database: {ex.Message}" });
            }
        }
    }
}
