using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ParallelTracker.Data;
using ParallelTracker.Models;
using ParallelTracker.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelTracker
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationContext>(options =>
                options.UseNpgsql(
                    Configuration.GetConnectionString("ApplicationConnection")));

            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddDefaultIdentity<User>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = Constants.AllowedUserNameCharacters;
            })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationContext>();

            services.AddControllersWithViews();

            services.AddAuthentication()
                .AddGitHub(options =>
                {
                    var configSection = Configuration.GetSection("GitHub");
                    options.ClientId = configSection["ClientId"];
                    options.ClientSecret = configSection["ClientSecret"];
                    options.Scope.Add("user:email");
                });
                //.AddGoogle(options =>
                //{
                //    var configSection = Configuration.GetSection("Google");
                //    options.ClientId = configSection["ClientId"];
                //    options.ClientSecret = configSection["ClientSecret"];
                //})

            services.AddScoped<CurrentResources>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCurrentResources();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/Identity/Account/Manage/DeletePersonalData", context =>
                    Task.Factory.StartNew(() => context.Response.Redirect("/", true)));
                endpoints.MapPost("/Identity/Account/Manage/DeletePersonalData", context =>
                    Task.Factory.StartNew(() => context.Response.Redirect("/", true)));

                endpoints.MapGet("/Identity/Account/Manage/DownloadPersonalData", context =>
                    Task.Factory.StartNew(() => context.Response.Redirect("/", true)));
                endpoints.MapPost("/Identity/Account/Manage/DownloadPersonalData", context =>
                    Task.Factory.StartNew(() => context.Response.Redirect("/", true)));

                endpoints.MapGet("/Identity/Account/Manage/PersonalData", context =>
                    Task.Factory.StartNew(() => context.Response.Redirect("/", true)));
                endpoints.MapPost("/Identity/Account/Manage/PersonalData", context =>
                    Task.Factory.StartNew(() => context.Response.Redirect("/", true)));

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Repos}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
