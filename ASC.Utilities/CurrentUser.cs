using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASC.Utilities
{
    namespace ASC.Utilities
    {
        public class CurrentUser
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public string[] Roles { get; set; } = Array.Empty<string>();
        }
    }
}
