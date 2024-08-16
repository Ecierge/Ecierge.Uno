namespace Ecierge.Uno.App.Services.Endpoints;

using System.Threading;
using System.Threading.Tasks;

using Refit;

[Headers("Content-Type: application/json")]
public interface IApiClient
{
    [Get("/api/weatherforecast")]
    Task<ApiResponse<IImmutableList<WeatherForecast>>> GetWeather(CancellationToken cancellationToken = default);
}
