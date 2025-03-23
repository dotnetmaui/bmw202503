using demo_maui_client.Services;
using Microsoft.Extensions.Logging;

namespace demo_maui_client;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // API 서버 주소 설정
        string apiBaseUrl = "https://yog880ks0gossskc4w0css44.jojangwon.com";

        // 서비스 등록
        builder.Services.AddSingleton(new AblyTokenService(apiBaseUrl));
        builder.Services.AddSingleton<AblyRealtimeService>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
	}
}
