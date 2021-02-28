using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using newDirectionTask_Backend.Models;

namespace newDirectionTask_Backend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.

        readonly string MyAllowSpecifiOrigins = "_myAllowSpecificOrigins";
        public void ConfigureServices(IServiceCollection services)
        {

            //i'll remove it later
            //services.AddDbContext<SearchContext>(opt =>
            //                                   opt.UseInMemoryDatabase("DatabaseTest"));

            //To Enable Cross-origin Request
            services.AddCors(options => {
                options.AddPolicy(name: MyAllowSpecifiOrigins,
                                builder =>
                                {
                                    builder.WithOrigins("http://localhost:3000")
                                    .AllowAnyHeader()
                                    .AllowAnyMethod();
                                });

            });


            //Database Connectionstring 
            services.AddDbContext<SearchContext>(opt =>
                                               opt.UseSqlServer(@"Server=.\SQLEXPRESS;Database=searchitemsdb2;Trusted_Connection=True;MultipleActiveResultSets=true"));
            services.AddControllers();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseHttpsRedirection();

            app.UseRouting();

            //must be placed after UseRouting and before UseAuthorization 
            app.UseCors(MyAllowSpecifiOrigins);

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
