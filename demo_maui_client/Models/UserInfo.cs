using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demo_maui_client.Models
{
    public class UserInfo
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }

        public string DisplayName => $"{(Role == "Admin" ? "관리자" : "일반 사용자")}";
    }
}
