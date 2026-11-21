namespace InstitutionalDiwan.Server.Infrastructure;

public static class SeaweedFsServiceCollectionExtensions
{
    public static IServiceCollection AddSeaweedFs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SeaweedFsOptions>(
            configuration.GetSection(SeaweedFsOptions.SectionName));
        services.AddSingleton<IHostedService, SeaweedFsHostedService>();
        return services;
    }
}
