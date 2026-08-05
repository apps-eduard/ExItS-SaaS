using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.IntegrationTests.Support;

internal static class GlobalCatalogTestServices
{
    public static ServiceProvider Build(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = connectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddPlatformPersistence(configuration);
        services.AddScoped<CreateGlobalProduct>();
        services.AddScoped<UpdateGlobalProduct>();
        services.AddScoped<CreateGlobalCategory>();
        services.AddScoped<UpdateGlobalCategory>();
        services.AddScoped<CreateCatalogTemplate>();
        services.AddScoped<CatalogTemplateQueryService>();
        services.AddScoped<GlobalProductQueryService>();
        services.AddScoped<AssignCatalogTemplateProduct>();
        services.AddScoped<RemoveCatalogTemplateProduct>();
        services.AddScoped<ReorderCatalogTemplateProducts>();
        services.AddScoped<UpdateCatalogTemplateProductFlags>();
        services.AddScoped<PublishCatalogTemplate>();
        services.AddSingleton<MutableClock>(new MutableClock(new DateTimeOffset(2026, 8, 6, 9, 0, 0, TimeSpan.Zero)));
        services.AddSingleton<IClock>(sp => sp.GetRequiredService<MutableClock>());

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Advances between operations so each composition change produces a distinct
    /// <c>UpdatedAtUtc</c>, matching how sequential admin requests behave.
    /// </summary>
    internal sealed class MutableClock(DateTimeOffset start) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = start;

        public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
    }
}
