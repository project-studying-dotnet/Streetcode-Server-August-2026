using System.Diagnostics.CodeAnalysis;
using Streetcode.BLL.Interfaces.Timeline;
using Streetcode.BLL.Services.Timeline;

namespace Streetcode.WebApi.Extensions;

public static class TimelineServiceCollectionExtensions
{
    [ExcludeFromCodeCoverage(Justification = "DI composition-root wiring; not meaningfully unit-testable")]
    public static void AddTimelineServices(this IServiceCollection services)
    {
        services.AddScoped<IHistoricalContextResolver, HistoricalContextResolver>();
    }
}
