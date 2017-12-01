using System;
using System.DirectoryServices.AccountManagement;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using LdapAuthentication.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LdapAuthentication.WebApi.Controllers
{

    [Authorize]
    //[Produces("application/json")]
    [Route("v1/accounts")]
    public class AccountsController : Controller
    {
        private readonly IConfigurationRoot _configuration;

        public AccountsController(IConfigurationRoot configuration)
        {
            _configuration = configuration;
        }


        [Authorize, HttpGet]
        [Route("test")]
        public IActionResult TestAuth()
        {
            //var claimsIdentity = User.Identity;
            return Ok(User.Identity.Name);
        }



        [AllowAnonymous]
        [HttpPost]
        [Route("token")]
        public IActionResult GenerateToken([FromBody] AuthModel authModel)
        {
            var domain = _configuration["Domain"];
            using (var pc = new PrincipalContext(ContextType.Domain, domain))
            {
                var isValid = pc.ValidateCredentials(authModel.UserName, authModel.Password);
                if (!isValid)
                {
                    return BadRequest();
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, authModel.UserName),
                    new Claim(JwtRegisteredClaimNames.Sub, authModel.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Tokens:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(_configuration["Tokens:Issuer"],
                    _configuration["Tokens:Issuer"],
                    claims,
                    expires: DateTime.Now.AddMinutes(int.Parse(_configuration["Tokens:Expires"])),
                    signingCredentials: creds);

                return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });


            }

        }






    }
}