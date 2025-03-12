using Business.Data;
using Business.Dto;
using Business.Models;
using Business.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Business.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly SubAdminServices _subAdminServices;
        private readonly BusinessContext _businessContext;
        private readonly IConfiguration _configuration;
        public AdminController(SubAdminServices subAdminServices, BusinessContext businessContext, IConfiguration configuration)
        {
            _subAdminServices = subAdminServices;
            _businessContext = businessContext;
            _configuration = configuration;
        }
        [HttpPost("add-sub-admin")]
        public async Task<IActionResult> AddSubAdmin(string email)
        {
            bool exists = await _businessContext.AdminLoginRequests.AnyAsync(u => u.EmailId == email);
            if (exists)
            {
                return Ok(new { messege = "duplicate" });
            }
            var defaultPassword =  await _subAdminServices.GetEmailSubAdmin(email);

            var adminLoginRequest = new AdminLoginRequest
            {
                EmailId = email,
                AdminPassword = defaultPassword,
                RoleId = 2,
                IsPasswordChanged = false
            };
            await _businessContext.AdminLoginRequests.AddAsync(adminLoginRequest);
            await _businessContext.SaveChangesAsync();
            // don't change value of messege property, as it's value is dependent on dialog box type in UI side
            return Ok(new { message = "success" });
        }

        [HttpGet("check-email")]
        public async Task<ActionResult<bool>> CheckEmailExists(string email)
        {
            bool exists = await _businessContext.AdminLoginRequests.AnyAsync(u => u.EmailId == email);
            return Ok(exists);
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest("Invalid request.");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(request.Token, tokenValidationParameters, out securityToken);
            var jwtToken = (JwtSecurityToken)securityToken;

            var emailIdfromToken = jwtToken.Claims.First(x => x.Type == "EmailId").Value;

            // Check if the user is a Business
            var admin = _businessContext.AdminLoginRequests.SingleOrDefault(u => u.EmailId == emailIdfromToken);
            if (admin == null)
            {
                return BadRequest("User not found.");
            }

            // Verify if the current password matches the stored password (default or user-provided)
            if (admin.AdminPassword != request.CurrentPassword)
            {
                return Unauthorized("Current password mismatch.");
            }

            if (admin.AdminPassword == request.NewPassword)
            {
                return BadRequest("new password should not be same as current password.");
            }

            // Validate new password - check with password requirement and should not be same with last
            if (request.NewPassword.Length < 6)
            {
                return BadRequest("New password does not meet the complexity requirements.");
            }

            // update the password in Admin table
            admin.EmailId = emailIdfromToken;
            admin.AdminPassword = request.NewPassword;
            admin.RoleId = 2;
            admin.IsPasswordChanged = true;
            var updateSuccess = _businessContext.AdminLoginRequests.Update(admin);
            if (updateSuccess == null)
            {
                return BadRequest("Failed to change password.");
            }
            await _businessContext.SaveChangesAsync();
            return Ok(new { message = "Password changed successfully." });
        }
    }
}
