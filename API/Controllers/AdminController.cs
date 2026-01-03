using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly DataContext _context;

        public AdminController(UserManager<User> userManager, DataContext context)
        {
            _userManager = userManager;
            _context = context;
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

            // Check if creditor with same email exists
            var existingCreditor = await _context.Creditors
                .FirstOrDefaultAsync(c => c.Email == createUserDto.Email);

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

            // If no creditor exists with this email, create one
            if (existingCreditor == null)
            {
                var newCreditor = new Creditor
                {
                    Name = createUserDto.DisplayName,
                    Email = createUserDto.Email
                };
                _context.Creditors.Add(newCreditor);
                await _context.SaveChangesAsync();
            }

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

            // Update corresponding creditor if exists
            var creditor = await _context.Creditors
                .FirstOrDefaultAsync(c => c.Email == user.Email);

            if (creditor != null)
            {
                creditor.Name = updateUserDto.DisplayName;
                creditor.Email = updateUserDto.Email;
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpPost("users/{id}/reset-password")]
        public async Task<ActionResult<string>> SendPasswordResetLink(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Set MustChangePassword flag
            user.MustChangePassword = true;
            await _userManager.UpdateAsync(user);

            // In a real application, you would send an email with a reset link
            // For now, we'll just return a message
            var message = $"Password reset link would be sent to {user.Email}. User must change password on next login.";

            return Ok(new { message });
        }
    }
}
