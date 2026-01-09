using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.DTOs;
using API.Services;
using Application.Interfaces;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{   [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly TokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AccountController(UserManager<User> userManager,
            SignInManager<User> signInManager,
            TokenService tokenService,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _tokenService = tokenService;
            _signInManager = signInManager;
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null) return Unauthorized();

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (result.Succeeded)
            {
                return await CreateUserObject(user);
            }
            return Unauthorized();
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await _userManager.Users.AnyAsync(x => x.Email == registerDto.Email))
            {
                return BadRequest("Email taken");
            }
            if (await _userManager.Users.AnyAsync(x => x.UserName == registerDto.UserName))
            {
                return BadRequest("Username taken");
            }

            var user = new User
            {
                DisplayName = registerDto.DisplayName, 
                Email = registerDto.Email, 
                UserName = registerDto.UserName
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                return await CreateUserObject(user);
            }

            return BadRequest("Problem registering user");



        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var username = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("unique_name");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return Unauthorized();

            return await CreateUserObject(user);
        }

        [Authorize]
        [HttpPost("changePassword")]
        public async Task<ActionResult> ChangePassword(ChangePasswordDto changePasswordDto)
        {
            var username = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("unique_name");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return Unauthorized();

            // Generate a password reset token for the current user
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Use the token to reset the password (no need for current password)
            var result = await _userManager.ResetPasswordAsync(user, token, changePasswordDto.NewPassword);

            if (result.Succeeded)
            {
                user.MustChangePassword = false;
                await _userManager.UpdateAsync(user);
                return Ok();
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest($"Failed to change password: {errors}");
        }

        [HttpPost("forgotPassword")]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordDto forgotPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);

            // Always return success to prevent email enumeration
            if (user == null)
            {
                return Ok(new { message = "If the email exists, a password reset link has been sent." });
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Create reset link
            var appUrl = _configuration["Email:AppUrl"] ?? "https://your-app-url.com";
            var resetLink = $"{appUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

            // Send email with reset link
            var emailSent = await _emailService.SendPasswordResetLinkAsync(
                user.Email,
                user.DisplayName,
                resetLink
            );

            return Ok(new { message = "If the email exists, a password reset link has been sent." });
        }

        [HttpPost("resetPassword")]
        public async Task<ActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);

            if (user == null)
            {
                return BadRequest("Invalid password reset token");
            }

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);

            if (result.Succeeded)
            {
                // Clear MustChangePassword flag when user resets their password
                user.MustChangePassword = false;
                await _userManager.UpdateAsync(user);
                return Ok(new { message = "Password has been reset successfully" });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest($"Failed to reset password: {errors}");
        }

        private async Task<UserDto> CreateUserObject(User user)
        {
            return new UserDto
                {
                    DisplayName = user.DisplayName,
                    Image = null,
                    Token = await _tokenService.CreateToken(user),
                    UserName = user.UserName,
                    MustChangePassword = user.MustChangePassword,
                    Roles = await _userManager.GetRolesAsync(user)
                };
        }


    }
}