namespace Ecierge.Uno.App.Services.Caching;

using System.Threading;
using System.Threading.Tasks;

public interface IWeatherCache
{
    ValueTask<IImmutableList<WeatherForecast>> GetForecast(CancellationToken token);
}
