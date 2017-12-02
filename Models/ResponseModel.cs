using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LdapAuthentication.WebApi.Models
{
    public class ResponseModel
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public JwtModel Data { get; set; }
    }
}
