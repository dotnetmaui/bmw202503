using demo_maui_client.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace demo_maui_client.Services
{
    public class AblyTokenService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public AblyTokenService(string apiBaseUrl)
        {
            _httpClient = new HttpClient();
            _apiBaseUrl = apiBaseUrl;
        }

        public async Task<string> GetAblyTokenAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/auth/gettoken?userId={userId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(content);
                    return tokenResponse.Token;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"토큰 요청 실패: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"토큰 서비스 오류: {ex.Message}", ex);
            }
        }

        public async Task<LoginResponse> LoginAsync(string userId, string password)
        {
            try
            {
                var loginRequest = new LoginParam
                {
                    UserId = userId,
                    Password = password
                };

                var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/auth/login", loginRequest);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<LoginResponse>(content);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"로그인 실패: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"로그인 서비스 오류: {ex.Message}", ex);
            }
        }
    }
}
