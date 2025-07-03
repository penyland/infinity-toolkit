using Infinity.Toolkit.EntityFramework;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace Infinity.Toolkit.IntegrationTests;

public sealed class SqlServerTestContainerFixture<T, TContext> : WebApplicationFactory<T>, IAsyncLifetime
    where T : class
    where TContext : DbContext
{
    public IConfiguration Configuration { get; private set; }

    private readonly MsSqlContainer msSqlContainer = new MsSqlBuilder()
      .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
      .Build();

    public override async ValueTask DisposeAsync()
    {
        await msSqlContainer.StopAsync();
        await msSqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await msSqlContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = $"{msSqlContainer.GetConnectionString()};MultipleActiveResultSets=true;";

        builder.ConfigureAppConfiguration((_, config) =>
        {
            Configuration = new ConfigurationBuilder()
          .AddJsonFile("appsettings.integrationtest.json")
          .Build();

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:Database", connectionString },
            });

            config.AddConfiguration(Configuration);
        })
        .ConfigureServices(async services =>
        {
            services.RemoveDbContext<TContext>();
            services.AddDbContext<TContext>(options =>
            {
                options
                    //.UseLazyLoadingProxies()
                    .UseSqlServer(connectionString);
            },
            ServiceLifetime.Singleton);

            var sp = services.BuildServiceProvider();
            var context = sp.CreateScope().ServiceProvider.GetRequiredService<TContext>();
            await SeedDatabaseAsync(context);
        })
        .UseEnvironment("IntegrationTest");
    }

    public static async Task SeedDatabaseAsync(TContext dbContext)
    {
        dbContext.Database.EnsureCreated();

        // START: Seed data if not already present
        // END: seed data here as needed

        await dbContext.SaveChangesAsync();
    }
}
