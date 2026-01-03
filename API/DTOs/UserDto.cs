using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.Collections.Generic;

namespace API.DTOs
{
    public class UserDto
    {
        public string DisplayName { get; set; }
        public string Token { get; set; }

        public string Image { get; set; }
        public string UserName { get; set; }
        public bool MustChangePassword { get; set; }
        public IList<string> Roles { get; set; }
    }
}