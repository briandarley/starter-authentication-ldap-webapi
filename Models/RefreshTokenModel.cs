using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LdapAuthentication.WebApi.Models
{
    public class RefreshTokenModel
    {
        public string ClientId { get; set; }
        public string RefreshToken { get; set; }
        public string Id { get; set; }
        public int IsStop { get; set; }
    }
}
