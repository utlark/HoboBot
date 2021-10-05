using Hangfire.SqlServer;
using Hangfire;
using HoboBot.Extensions;
using HoboBot.HangfireAuthentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using VkNet.Abstractions;
using VkNet.Model;
using VkNet;


namespace HoboBot
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson(sp => sp.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

            services.AddDbContext<VkBotDBContext>(sp =>
            {
                sp.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
                sp.UseLazyLoadingProxies(false);
            });

            services.AddSingleton<IVkApi>(sp =>
            {
                VkApi vkApi = new();
                vkApi.Authorize(new ApiAuthParams { AccessToken = Configuration["Config:AccessToken"] });
                return vkApi;
            });

            services.AddSingleton(sp =>
            {
                return new CryptoRandom();
            });

            services.AddHangfire(sp =>
            {
                sp.UseSqlServerStorage(Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true,
                    JobExpirationCheckInterval = TimeSpan.FromMinutes(30),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    DashboardJobListLimit = 10000,
                    TransactionTimeout = TimeSpan.FromMinutes(1),
                });
                sp.UseSerializerSettings(new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            });

            services.AddHangfireServer();

            services.AddControllersWithViews().AddNewtonsoftJson(sp => sp.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDefaultFiles();

            app.UseStaticFiles();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                DashboardTitle = "HoboBot",
                Authorization = new[] { new HangfireCustomFilter{
                    User = Configuration["HangfireSettings:UserName"],
                    Pass = Configuration["HangfireSettings:Password"]
                } }
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
