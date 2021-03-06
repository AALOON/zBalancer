﻿using System;
using System.IO;
using System.Reflection;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using zBalancer.Balancer.Context;
using zBalancer.Balancer.Keys;
using zBalancer.Balancer.Middlewares;
using zBalancer.Balancer.Repositories;
using zBalancer.Balancer.Services;

namespace zBalancer.Balancer
{
    /// <summary>
    /// The Startup class configures services and the app's request pipeline.
    /// </summary>
    public class Startup
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// The Startup .ctor for di the IConfiguration
        /// </summary>
        /// <param name="config"></param>
        public Startup(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Setups services to the container.
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            // Auto Mapper Configurations
            var mappingConfig = new MapperConfiguration(cfg => {
                cfg.AddProfiles(typeof(Startup));
            });

            IMapper mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            var connectionString = _config.GetConnectionString("NodesDatabase");
            services.AddDbContext<NodeContext>
                (options => options.UseSqlite(connectionString), ServiceLifetime.Singleton);

            services.AddOptions();

            services.AddMemoryCache();

            services.AddSingleton<INodeRepository, NodeRepository>();
            services.AddSingleton<INodeService, NodeService>();
            services.AddSingleton<INodeSelectionService, RoundRobinSelectionService>();

            services.AddSingleton<ForwardMiddleware>();
            services.AddSingleton<BalancerMiddleware>();
            services.AddSingleton<RequestSenderMiddleware>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Nodes API of balancer server", Version = "v1" });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            // Set the comments path for the Swagger JSON and UI.

        }

        /// <summary>
        /// Called by the runtime.This method configures the HTTP request pipeline.
        /// </summary>
        public void Configure(IApplicationBuilder appBuilder, IHostingEnvironment env)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            appBuilder.UseSwagger(c =>
            {
                c.RouteTemplate = $"{Routes.Api}/swagger/{{documentName}}/swagger.json";
            });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            appBuilder.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/{Routes.Api}/swagger/v1/swagger.json", "Nodes API of balancer server V1");
                c.RoutePrefix = $"{Routes.Api}/swagger";
            });

            if (env.IsDevelopment())
            {
                appBuilder.UseDeveloperExceptionPage();
            }

            appBuilder.MapWhen(context => !context.Request.Path.StartsWithSegments(Routes.NodesApi), appBuilderWhen =>
            {
                appBuilderWhen.UseMiddleware<ForwardMiddleware>();
                appBuilderWhen.UseMiddleware<BalancerMiddleware>();
                appBuilderWhen.UseMiddleware<RequestSenderMiddleware>();
            });

            appBuilder.UseMvc();
        }
    }
}
