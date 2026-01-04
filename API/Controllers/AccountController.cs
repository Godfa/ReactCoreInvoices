using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.DTOs;
using API.Services;
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
        public AccountController(UserManager<User> userManager,        
       
        SignInManager<User> signInManager, TokenService tokenService) 
        {
            _tokenService = tokenService;
            _signInManager = signInManager;
            _userManager = userManager;
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

            var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);

            if (result.Succeeded)
            {
                user.MustChangePassword = false;
                await _userManager.UpdateAsync(user);
                return Ok();
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest($"Failed to change password: {errors}");
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