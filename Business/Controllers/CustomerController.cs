﻿using Business.Data;
using Business.Dto;
using Business.Models;
using Business.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Business.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly BusinessContext _context;
        public ILogger<CustomerController> _logger;
        private readonly IConfiguration _configuration;

        public CustomerController(ILogger<CustomerController> logger, BusinessContext context, HttpClient httpClient, IConfiguration configuration, GeocodingService geocodingService)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<bool>> Registercustomer(Customer customer)
        {
            // Validate customer data
            if (customer == null || string.IsNullOrEmpty(customer.Cus_EmailId) || string.IsNullOrEmpty(customer.Cus_Password))
            {
                return BadRequest(new { message = "Invalid data. Please check the input." }); // HTTP 400 Bad Request
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(customer.Cus_Password);

            try
            {
                // Add new customer
                var customerObj = new Customer
                {
                    Cus_EmailId = customer.Cus_EmailId,
                    Cus_Password = hashedPassword,
                    Cus_Location = customer.Cus_Location,
                    Longitude = customer.Longitude,
                    Latitude = customer.Latitude,
                    RoleID = 4
                };
                _context.Customers.Add(customerObj);
                await _context.SaveChangesAsync();
                return Ok(new { data = "pass" });
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while processing your request.", details = ex.Message }); // HTTP 500 Internal Server Error
            }
        }

        [HttpGet("getcusdetailsbyid")]
        public async Task<ActionResult> getCustomerDetailByID(int cusId)
        {
            var customerData = await _context.Customers.Where(u => u.Cus_Id == cusId).ToListAsync();
            return Ok(customerData);
        }

        [HttpGet("check-email")]
        public async Task<ActionResult<bool>> CheckEmailExists(string email)
        {
            bool exists = await _context.Customers.AnyAsync(u => u.Cus_EmailId == email);
            return Ok(exists);
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest loginRequest)
        {
            var usercustomer = _context.Customers.FirstOrDefault(u => u.Cus_EmailId == loginRequest.Username && u.Cus_Password == loginRequest.Password);
            if (usercustomer == null)
                return Unauthorized();

            var token = GenerateToken(loginRequest.Username, loginRequest.RememberMe);
            return Ok(new LoginResponse
            {
                Token = token.Token,
                Expiration = token.Expiration
            });
        }
        private (string Token, DateTime Expiration) GenerateToken(string email, bool rememberMe)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = rememberMe ? DateTime.Now.AddDays(30) : DateTime.Now.AddHours(1); // Longer expiration for Remember Me

            var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: new[] { new Claim(ClaimTypes.Email, email) },
            expires: expiration,
            signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiration);
        }
    }
}
