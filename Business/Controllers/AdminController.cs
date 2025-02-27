using Business.Data;
using Business.Models;
using Business.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly SubAdminServices _subAdminServices;
        private readonly BusinessContext _businessContext;
        public AdminController(SubAdminServices subAdminServices, BusinessContext businessContext)
        {
            _subAdminServices = subAdminServices;
            _businessContext = businessContext;
        }
        [HttpPost("add-sub-admin")]
        public async Task<IActionResult> AddSubAdmin(string email)
        {
           var defaultPassword =  await _subAdminServices.GetEmailSubAdmin(email);

            var adminLoginRequest = new AdminLoginRequest
            {
                EmailId = email,
                AdminPassword = defaultPassword,
                RoleId = 2
            };
            await _businessContext.AdminLoginRequests.AddAsync(adminLoginRequest);
            await _businessContext.SaveChangesAsync();
            Console.WriteLine("Data inserted successfully.");
            return Ok(new { message = "Sub-admin added and email sent." });
        }

        [HttpGet("check-email")]
        public async Task<ActionResult<bool>> CheckEmailExists(string email)
        {
            bool exists = await _businessContext.AdminLoginRequests.AnyAsync(u => u.EmailId == email);
            return Ok(exists);
        }

    }
}
