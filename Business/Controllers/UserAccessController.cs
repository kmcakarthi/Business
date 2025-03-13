using Business.Data;
using Business.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAccessController : ControllerBase
    {
        private readonly BusinessContext _businessContext;

        public UserAccessController(BusinessContext businessContext)
        {
            _businessContext = businessContext;
        }
        [HttpGet]
        [Route("getall-customer")]
        public async Task<IActionResult> GetAllCustomers()
        {
            IQueryable<Customer> query = _businessContext.Customers;
            var result = await query
                               .Select(c => new { c.Cus_Id, c.Cus_EmailId, c.RoleID })
                               .ToListAsync();

            return Ok(result);
        }
    }
}
