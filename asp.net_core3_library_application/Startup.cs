using LibraryData;
using LibraryServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace asp.net_core3_library_application
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
            // AddMVC
            services.AddControllersWithViews();
            services.AddSingleton(this.Configuration);
            // Library service (dependency) injection from ILibraryAsset requests
            services.AddScoped<ILibraryAsset, LibraryAssetService>();
            services.AddScoped<ICheckout, CheckoutService>();
            services.AddScoped<IPatron, PatronService>();
            services.AddScoped<ILibraryBranch, LibraryBranchService>();
            // Gets the configuration string from the appsettings.json using msoft efc
            services.AddDbContext<LibraryContext>(options => options.UseSqlServer(this.Configuration.GetConnectionString("LibraryConnection")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
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

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
