using System.ComponentModel.DataAnnotations;

namespace LdapAuthentication.WebApi.Models
{
    public class AuthModel
    {
        [Key]
        public string ClientId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public string GrantType { get; set; }
        public string RefreshToken { get; set; }
        
        public string ClientSecret { get; set; }
    }
}
