using demo_api_tokengenerator.Models;
using IO.Ably;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace demo_api_tokengenerator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        // 데모용 더미 사용자 리스트
        private readonly List<User> _userList = new List<User>
        {
            new User { UserId = "user1@bmw.net", Password = "password1", Name = "마우이", Role = "Admin" },
            new User { UserId = "user2@bmw.net", Password = "password2", Name = "크로스", Role = "User" },
            new User { UserId = "user3@bmw.net", Password = "password3", Name = "플랫폼", Role = "User" }
        };

        private string apiKey = "zotlFQ.5JukjQ:eTytr6TVCPFUCnUvo-T7iGp7cO8fZalcLLSjwTWafkw"; //꼭 바꾸세요!

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginParam request)
        {
            // 사용자 인증
            var user = _userList.SingleOrDefault(u => u.UserId == request.UserId);

            if (user == null)
            {
                return Unauthorized(new { message = "사용자가 존재하지 않습니다." });
            }

            if (user.Password != request.Password)
            {
                return Unauthorized(new { message = "비밀번호가 일치하지 않습니다." });
            }

            // 인증 성공 시 Ably 토큰 생성
            var ablyToken = GetAblyToken(user);

            return Ok(new
            {
                user = new { user.UserId, user.Name, user.Role },
                token = ablyToken
            });
        }

        [HttpGet("gettoken")]
        public IActionResult GetAblyTokenForUser([FromQuery] string userId)
        {
            var user = _userList.SingleOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                return Unauthorized(new { message = "사용자가 존재하지 않습니다." });
            }

            var ablyToken = GetAblyToken(user);
            return Ok(new { token = ablyToken });
        }

        private string GetAblyToken(User user)
        {
            try
            {
                // Ably API 키 가져오기 (appsettings.json에 저장된 값)
                //string apiKey = _configuration["Ably:ApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new Exception("Ably API 키가 구성되지 않았습니다.");
                }

                // Ably 클라이언트 초기화
                var clientOptions = new ClientOptions(apiKey)
                {
                    QueryTime = true
                };

                var rest = new AblyRest(clientOptions);

                // 토큰 요청 생성
                var tokenParams = new TokenParams
                {
                    ClientId = user.UserId,
                    Capability = new Capability(GetCapabilityForRole(user.Role)),
                    Ttl = TimeSpan.FromHours(12) // 토큰 만료 시간을..이리 길게 주면 안되겠죠?
                };

                // 토큰 요청
                var tokenDetails = rest.Auth.RequestToken(tokenParams);

                return tokenDetails.Token;
            }
            catch (Exception ex)
            {
                // 실제 애플리케이션에서는 로깅 처리
                Console.WriteLine($"Ably 토큰 생성 오류: {ex.Message}");
                throw new Exception("Ably 토큰을 생성하는 중 오류가 발생했습니다.", ex);
            }
        }

        private string GetCapabilityForRole(string role)
        {
            // 역할에 따른 채널 권한 설정
            if (role == "Admin")
            {
                return "{ \"*\": [\"publish\", \"subscribe\", \"presence\", \"history\"] }";
            }
            else
            {
                return "{ \"public:*\": [\"subscribe\"], \"user:*\": [\"publish\", \"subscribe\"] }";
            }
        }
    }
}
