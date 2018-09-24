using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using zBalancer.Balancer.Context;
using zBalancer.Balancer.Middlewares;
using zBalancer.Balancer.Repositories;
using zBalancer.Balancer.Services;

namespace zBalancer.Balancer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Auto Mapper Configurations
            var mappingConfig = new MapperConfiguration(cfg => {
                cfg.AddProfiles(typeof(Startup));
            });

            IMapper mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            var connectionString = "Data Source=blogging.db";
            services.AddDbContext<NodeContext>
                (options => options.UseSqlite(connectionString), ServiceLifetime.Singleton);

            services.AddOptions();

            services.AddMemoryCache();

            services.AddSingleton<INodeRepository, NodeRepository>();
            services.AddSingleton<INodeService, NodeService>();

            services.AddSingleton<ForwardMiddleware>();
            services.AddSingleton<BalancerMiddleware>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder appBuilder, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                appBuilder.UseDeveloperExceptionPage();
            }

            appBuilder.MapWhen(context => !context.Request.Path.StartsWithSegments(Routes.NodesApi), appBuilderWhen =>
            {
                appBuilderWhen.UseMiddleware<ForwardMiddleware>();
                appBuilderWhen.UseMiddleware<BalancerMiddleware>();
            });

            appBuilder.UseMvc();
        }
    }
}
