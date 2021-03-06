﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using CloudService.Common;
using CloudService.Common.Configuration;
using CloudService.Service.WebApi;
using CloudService.Service.WorkTask;
using CloudService.Infrastructure;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using NLog.Extensions.Logging;

namespace CloudService.Host
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            this.Configuration = builder.Build();
        }

        public IContainer ApplicationContainer { get; private set; }

        public IConfigurationRoot Configuration { get; private set; }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Add services to the collection.
            services.AddMvc(config =>
            {
                //config.Filters.Add(new CustomExceptionFilter());
            })
            .AddJsonOptions(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                options.SerializerSettings.Converters.Add(new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" });
            })
            .AddControllersAsServices();

            services.AddCors(options =>
            {
                options.AddPolicy("TestServer",
                    policy => policy.WithOrigins("http://localhost:5000"));
            });
            var builder = new ContainerBuilder();
            var appCfg = new AppSettings(Configuration);
            //TODO: contact manage api to grab the predefined configurations. If it failed or undefined, use local settings instead.

            builder.RegisterInstance(appCfg).SingleInstance();

            var taskManager = new TaskManager();
            builder.Register(mgr => taskManager).SingleInstance();

            builder.Populate(services);

            builder.RegisterType<TestController>().PropertiesAutowired();

            this.ApplicationContainer = builder.Build();

            DependencyResolver.SetContainer(ApplicationContainer);
            return new AutofacServiceProvider(this.ApplicationContainer);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            loggerFactory.AddNLog();
            app.UseMvc();

            app.UseCors("TestServer");
            appLifetime.ApplicationStopped.Register(() => this.ApplicationContainer.Dispose());
        }
    }
}
