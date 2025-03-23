using IO.Ably;
using IO.Ably.Realtime;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace demo_maui_client.Services
{
    public class AblyRealtimeService    
    {
        private AblyRealtime _realtimeClient;
        private IRealtimeChannel _channel;
        private readonly AblyTokenService _tokenService;
        private readonly ILogger<AblyRealtimeService> _logger;

        // 이벤트 정의
        public event Action<ConnectionState, ErrorInfo> ConnectionStateChanged;
        public event Action<string, object> MessageReceived;

        public AblyRealtimeService(AblyTokenService tokenService, ILogger<AblyRealtimeService> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task InitializeAsync(string userId)
        {
            try
            {
                var token = await _tokenService.GetAblyTokenAsync(userId);

                var clientOptions = new ClientOptions
                {
                    Token = token,
                    AutoConnect = false,
                    LogLevel = IO.Ably.LogLevel.Debug
                };

                _realtimeClient = new AblyRealtime(clientOptions);

                // 연결 상태 변경 이벤트 등록 및 전달
                _realtimeClient.Connection.On(args =>
                {
                    // 상태 변경 이벤트 발생
                    ConnectionStateChanged?.Invoke(args.Current, args.Reason);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ably 초기화 오류: {ex.Message}");
                throw;
            }
        }

        public void Connect()
        {
            _realtimeClient?.Connect();
        }

        public void Disconnect()
        {
            _realtimeClient?.Close();
        }

        public IRealtimeChannel SubscribeToChannel(string channelName, bool useRewind = false, string rewindValue = "1")
        {
            // 기존 채널이 있으면 구독 해제
            if (_channel != null)
            {
                _channel.Unsubscribe();  // 모든 리스너 제거
                _channel.Detach();       // 채널에서 완전히 분리
            }

            // 채널 옵션 설정
            IO.Ably.ChannelOptions channelOptions = null;

            if (useRewind)
            {
                var channelParams = new IO.Ably.ChannelParams();
                channelParams.Add("rewind", rewindValue); // 메시지 수 또는 시간 단위(예: "1m", "5s")
                channelOptions = new IO.Ably.ChannelOptions();
                channelOptions.Params = channelParams;
            }

            // 채널 가져오기
            _channel = channelOptions != null
                ? _realtimeClient.Channels.Get(channelName, channelOptions)
                : _realtimeClient.Channels.Get(channelName);

            //_channel = _realtimeClient.Channels.Get(channelName);

            // 메시지 수신 이벤트 등록 및 전달
            _channel.Subscribe(message =>
            {
                // 메시지 수신 이벤트 발생
                MessageReceived?.Invoke(message.Name, $"{message.ClientId} : {message.Data}");
            });


            return _channel;
        }

        public async Task PublishMessageAsync(string eventName, object data)
        {
            if (_channel == null)
            {
                throw new InvalidOperationException("채널에 먼저 구독해야 합니다.");
            }

            await _channel.PublishAsync(eventName, data);
        }
    }
}
