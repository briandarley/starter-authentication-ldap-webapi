using System;
using System.Text;
using LdapAuthentication.WebApi.Infrastructure;
using LdapAuthentication.WebApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace LdapAuthentication.WebApi
{
    public class Startup
    {
        private IConfigurationRoot _configuration;

        public Startup(IHostingEnvironment env)
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .Build();
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {

            ConfigureJwtAuthService(services);

            services.AddMvc()
                .AddJsonOptions(
                    options =>
                    {
                        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                        options.SerializerSettings.Formatting = Formatting.Indented;
                    }
            );
            services.AddDbContext<ApiContext>(opt => opt.UseInMemoryDatabase("appContext"));
            services.AddSingleton(_configuration);
            services.AddAuthorization();


            // Build the intermediate service provider
            var serviceProvider = services.BuildServiceProvider();
            //return the provider
            return serviceProvider;


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
                app.UseDatabaseErrorPage();
            }

            app.UseAuthentication();

            var context = serviceProvider.GetService<ApiContext>();
            SeedDatabase(context);

            app.UseMvc();


        }

        private void SeedDatabase(ApiContext context)
        {

            var user1 = new AuthModel
            {
                ClientId = "123",
                //GrantType = "password",
                ClientSecret = "secret",
                Password = "password",
                UserName = "bdarley"
            };


            context.Users.Add(user1);

            context.SaveChanges();
        }


        public void ConfigureJwtAuthService(IServiceCollection services)
        {
            var audienceConfig = _configuration.GetSection("Tokens");
            var symmetricKeyAsBase64 = audienceConfig["Key"];
            var keyByteArray = Encoding.ASCII.GetBytes(symmetricKeyAsBase64);
            var signingKey = new SymmetricSecurityKey(keyByteArray);

            var tokenValidationParameters = new TokenValidationParameters
            {
                // The signing key must match!  
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,

                // Validate the JWT Issuer (iss) claim  
                ValidateIssuer = true,
                ValidIssuer = audienceConfig["Issuer"],

                // Validate the JWT Audience (aud) claim  
                ValidateAudience = true,
                ValidAudience = audienceConfig["Audience"],

                // Validate the token expiry  
                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero
            };

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                //AddJwtBearerAuthentication
                .AddJwtBearer(cfg =>
                {
                    cfg.TokenValidationParameters = tokenValidationParameters;
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;

                    cfg.TokenValidationParameters = new TokenValidationParameters()
                    {
                        
                        ValidIssuer = _configuration["Tokens:Issuer"],
                        ValidAudience = _configuration["Tokens:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Tokens:Key"]))
                    };
                });
        }
    }
}
