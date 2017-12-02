using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LdapAuthentication.WebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LdapAuthentication.WebApi.Infrastructure
{

    public class ApiContext : DbContext
    {
        private readonly string _name;

        public ApiContext(string name)
        {
            _name = name;
        }
        public ApiContext(DbContextOptions<ApiContext> options)
            : base(options)
        {
        }

        public DbSet<AuthModel> Users { get; set; }
        //
        public DbSet<RefreshTokenModel> RefreshTokens { get; set; }
        public bool AddToken(RefreshTokenModel rToken)
        {
            RefreshTokens.Add(rToken);
            SaveChanges();
            return true;
        }

        public RefreshTokenModel GetToken(string refreshToken, string clientId)
        {
            var result = RefreshTokens.FirstOrDefault(c => c.RefreshToken == refreshToken && c.ClientId == clientId);
            return result;
        }

        public bool ExpireToken(RefreshTokenModel token)
        {
            RefreshTokens.Remove(token);
            SaveChanges();
            return true;
        }
    }
}
