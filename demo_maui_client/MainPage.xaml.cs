using demo_maui_client.Services;
using IO.Ably;
using Microsoft.Extensions.Logging;

namespace demo_maui_client
{
    public partial class MainPage : ContentPage
    {

        private readonly AblyTokenService _tokenService;
        private readonly AblyRealtimeService _ablyService;
        private string _currentToken;
        private string _currentUserId;
        private ILogger<MainPage> _logger;
        private bool UseMessageHistory => HistorySwitch.IsToggled;
        private bool UseRewind => RewindSwitch.IsToggled;

        public MainPage(AblyTokenService tokenService, AblyRealtimeService ablyService, ILogger<MainPage> logger)
        {
            InitializeComponent();

            _tokenService = tokenService;
            _ablyService = ablyService;
            _logger = logger;

            // 기본적으로 첫 번째 항목 선택 (선택하세요)
            DemoUserPicker.SelectedIndex = 0;
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            try
            {
                string userId = UserId.Text;
                string password = Password.Text;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
                {
                    await DisplayAlert("오류", "사용자 ID와 비밀번호를 입력하세요.", "확인");
                    return;
                }

                LoginButton.IsEnabled = false;
                LoginButton.Text = "로그인 중...";

                _logger.LogInformation($"로그인 시도: {userId}");
                var loginResponse = await _tokenService.LoginAsync(userId, password);

                _currentToken = loginResponse.Token;
                _currentUserId = loginResponse.User.UserId;

                // 사용자 정보 표시
                UserNameLabel.Text = $"이름: {loginResponse.User.Name}";
                UserRoleLabel.Text = $"역할: {loginResponse.User.DisplayName}";
                TokenLabel.Text = $"토큰: {_currentToken}";

                UserInfoFrame.IsVisible = true;
                AblyFrame.IsVisible = true;

                _logger.LogInformation($"로그인 성공: {loginResponse.User.Name}");
                await DisplayAlert("성공", "로그인 성공!", "확인");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "로그인 실패");
                await DisplayAlert("오류", $"로그인 실패: {ex.Message}", "확인");
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Text = "로그인";
            }
        }

        private void OnDemoUserSelected(object sender, EventArgs e)
        {
            // 선택된 인덱스에 따라 사용자 정보 설정
            switch (DemoUserPicker.SelectedIndex)
            {
                case 1: // 데모 1: 마우이 (관리자)
                    UserId.Text = "user1@bmw.net";
                    Password.Text = "password1";
                    break;
                case 2: // 데모 2: 크로스 (일반 사용자)
                    UserId.Text = "user2@bmw.net";
                    Password.Text = "password2";
                    break;
                case 3: // 데모 3: 플랫폼 (일반 사용자)
                    UserId.Text = "user3@bmw.net";
                    Password.Text = "password3";
                    break;
                default:
                    // 기본값 또는 초기화
                    UserId.Text = string.Empty;
                    Password.Text = string.Empty;
                    break;
            }
        }


        private async void OnConnectClicked(object sender, EventArgs e)
        {
            try
            {
                ConnectButton.IsEnabled = false;
                ConnectButton.Text = "연결 중...";

                _logger.LogInformation("Ably 연결 시도");
                await _ablyService.InitializeAsync(_currentUserId);

                //// 연결 상태 변경 이벤트 등록
                _ablyService.ConnectionStateChanged += (state, reason) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _logger.LogInformation($"Ably 연결 상태 변경: {state}");
                        if (state == IO.Ably.Realtime.ConnectionState.Connected)
                        {
                            DisconnectButton.IsEnabled = true;
                            SendButton.IsEnabled = true;
                            ConnectButton.Text = "연결됨";
                        }
                        else if (state == IO.Ably.Realtime.ConnectionState.Failed || state == IO.Ably.Realtime.ConnectionState.Disconnected)
                        {
                            DisconnectButton.IsEnabled = false;
                            SendButton.IsEnabled = false;
                            ConnectButton.IsEnabled = true;
                            ConnectButton.Text = "Ably 연결";
                        }
                    });
                };

                _ablyService.Connect();

                var channelName = ChannelName.Text; 
                bool useRewind = RewindSwitch.IsToggled;
                bool useHistory = HistorySwitch.IsToggled;

                _logger.LogInformation($"채널 구독: {channelName}, 되감기 사용: {useRewind}");

                var channel = _ablyService.SubscribeToChannel(channelName, useRewind, "1");

                if (useHistory)
                {
                    var resultPage = await channel.HistoryAsync(new PaginatedRequestParams { Direction = QueryDirection.Forwards });
                    foreach (var message in resultPage.Items)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            _logger.LogInformation($"메시지 수신: {message.Name} - {message.Data}");
                            ReceivedMessagesLabel.Text += $"\n[{message.Name} 타입] {message.Data}";
                        });
                    }
                }

                _ablyService.MessageReceived -= OnMessageReceived; // 기존 핸들러 제거
                _ablyService.MessageReceived += OnMessageReceived; // 새로 핸들러 등록


                await DisplayAlert("정보", "Ably에 연결 중입니다. 연결이 완료되면 버튼이 활성화됩니다.", "확인");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ably 연결 실패");
                await DisplayAlert("오류", $"Ably 연결 실패: {ex.Message}", "확인");
                ConnectButton.IsEnabled = true;
                ConnectButton.Text = "Ably 연결";
            }
        }

        private void OnMessageReceived(string name, object data)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _logger.LogInformation($"메시지 수신: {name} - {data}");
                ReceivedMessagesLabel.Text += $"\n[{name} 타입] {data}";
            });
        }

        private void OnDisconnectClicked(object sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Ably 연결 해제");
                _ablyService.Disconnect();

                DisconnectButton.IsEnabled = false;
                SendButton.IsEnabled = false;
                ConnectButton.IsEnabled = true;
                ConnectButton.Text = "Ably 연결";

                DisplayAlert("성공", "Ably 연결이 해제되었습니다.", "확인");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ably 연결 해제 실패");
                DisplayAlert("오류", $"Ably 연결 해제 실패: {ex.Message}", "확인");
            }
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            try
            {
                string message = MessageInput.Text;

                if (string.IsNullOrEmpty(message))
                {
                    await DisplayAlert("오류", "메시지를 입력하세요.", "확인");
                    return;
                }

                _logger.LogInformation($"메시지 전송: {message}");
                await _ablyService.PublishMessageAsync("message", message);
                MessageInput.Text = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메시지 전송 실패");
                await DisplayAlert("오류", $"메시지 전송 실패: {ex.Message}", "확인");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // 페이지를 떠날 때 연결 해제
            _ablyService?.Disconnect();
        }
    }
}
