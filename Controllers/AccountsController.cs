using System;
using System.DirectoryServices.AccountManagement;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using LdapAuthentication.WebApi.Infrastructure;
using LdapAuthentication.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LdapAuthentication.WebApi.Controllers
{

    [Authorize]
    [Route("v1/accounts")]
    public class AccountsController : Controller
    {
        private readonly IConfigurationRoot _configuration;
        private readonly ApiContext _apicontext;

        public AccountsController(IConfigurationRoot configuration, ApiContext apicontext)
        {
            _configuration = configuration;
            _apicontext = apicontext;
        }

        /// <summary>
        /// Test to check if Auth Works
        /// </summary>
        /// <returns></returns>
        [Authorize, HttpGet]
        [Route("test")]
        public IActionResult TestAuth()
        {
            //var claimsIdentity = User.Identity;
            return Ok(User.Identity.Name);
        }


        /// <summary>
        /// Sample Authenticate Via LDAP
        /// </summary>
        /// <param name="authModel"></param>
        /// <returns></returns>
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




      
        
        /// <summary>
        /// Authenticate users against an in-memory database, also returns a refresh token 
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [AllowAnonymous, HttpPost("auth")]
        public IActionResult Auth([FromBody]AuthModel parameters)
        {
            if (parameters == null)
            {
                return BadRequest("Invalid Request");
            }


            switch (parameters.GrantType)
            {
                case "password":
                    return Ok(ResponseByPassword(parameters));
                case "refreshToken":
                    return Ok(ResponseByRefreshToken(parameters));
                default:
                    return BadRequest("Invalid Request, invalid grant type");
            }
        }

        //scenario 1 ï¼š get the access-token by username and password  
        private ResponseModel ResponseByPassword(AuthModel parameters)
        {

            var user = _apicontext.Users.FirstOrDefault(x => x.ClientId == parameters.ClientId
                                    && x.ClientSecret == parameters.ClientSecret
                                    && x.UserName == parameters.UserName
                                    && x.Password == parameters.Password);

            if (user == null)
            {
                return new ResponseModel
                {
                    Code = "902",
                    Message = "invalid user information",
                    Data = null
                };
            }

            var refreshToken = Guid.NewGuid().ToString().Replace("-", "");

            var rToken = new RefreshTokenModel
            {
                ClientId = parameters.ClientId,
                RefreshToken = refreshToken,
                Id = Guid.NewGuid().ToString(),
                IsStop = 0
            };

            //store the refreshToken   
            return _apicontext.AddToken(rToken)
                ? new ResponseModel
                {
                    Code = "999",
                    Message = "OK",
                    Data = GetJwt(user, refreshToken)
                }
                : new ResponseModel
                {
                    Code = "909",
                    Message = "can not add token to database",
                    Data = null
                };
        }

        //scenario 2 ï¼š get the access_token by refreshToken  
        private ResponseModel ResponseByRefreshToken(AuthModel parameters)
        {
            var user = _apicontext.Users.FirstOrDefault(x => x.ClientId == parameters.ClientId);
            var token = _apicontext.GetToken(parameters.RefreshToken, parameters.ClientId);

            if (token == null)
            {
                return new ResponseModel
                {
                    Code = "905",
                    Message = "can not refresh token",
                    Data = null
                };
            }

            if (token.IsStop == 1)
            {
                return new ResponseModel
                {
                    Code = "906",
                    Message = "refresh token has expired",
                    Data = null
                };
            }

            var refreshToken = Guid.NewGuid().ToString().Replace("-", "");

            token.IsStop = 1;
            //expire the old refreshToken and add a new refreshToken  
            var updateFlag = _apicontext.ExpireToken(token);

            var addFlag = _apicontext.AddToken(new RefreshTokenModel
            {
                ClientId = parameters.ClientId,
                RefreshToken = refreshToken,
                Id = Guid.NewGuid().ToString(),
                IsStop = 0
            });

            return updateFlag && addFlag
                ? new ResponseModel
                {
                    Code = "999",
                    Message = "OK",
                    Data = GetJwt(user, refreshToken)
                }
                : new ResponseModel
                {
                    Code = "910",
                    Message = "can not expire token or a new token",
                    Data = null
                };
        }


        private JwtModel GetJwt(AuthModel authModel, string refreshToken)
        {
            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(int.Parse(_configuration["Tokens:Expires"]));
            var claims = new[]
            {
                 new Claim(ClaimTypes.Name, authModel.UserName),
            new Claim(JwtRegisteredClaimNames.Sub, authModel.ClientId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUniversalTime().ToString(), ClaimValueTypes.Integer64)
            };


            var keyByteArray = Encoding.ASCII.GetBytes(_configuration["Tokens:Key"]);
            var signingKey = new SymmetricSecurityKey(keyByteArray);

            var jwt = new JwtSecurityToken(
                issuer: _configuration["Tokens:Issuer"],
                audience: _configuration["Tokens:Audience"],
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            var response = new JwtModel
            {
                AccessToken = encodedJwt,
                ExpiresIn = (expires - now).TotalSeconds,
                RefreshToken = refreshToken
            };
            return response;
            
        }
    }
}