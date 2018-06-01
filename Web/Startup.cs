using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using GitLab;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Nancy.Owin;
using Newtonsoft.Json.Linq;
using NHibernate;
using NHibernate.Dialect;
using NHibernate.Mapping.ByCode;
using RepositoryApi;
using Web.Modules.Db;

namespace Web
{
    public class Startup
    {
        private IContainer _container;
        public IHostingEnvironment HostingEnvironment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IHostingEnvironment env, IConfiguration config)
        {
            HostingEnvironment = env;
            Configuration = config;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddIdentityCore<ReviewUser>(options => { })
                .AddUserStore<NHibernateUserStore>();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = "GitHub";
                })
                .AddCookie()
                .AddOAuth("GitHub", options =>
                {
                    options.ClientId = "115b5081de93b0a610af4c4dcc5812ad72de81c6fa15cfa7a2191c8e836bf21a";
                    options.ClientSecret = "12b1724fb7a57b640f93f2e8dac7a9e41964e0bf0bdf68f9d7c42bdf888e35f7";
                    options.CallbackPath = new PathString("/signin-github");

                    options.AuthorizationEndpoint = "https://git.kplabs.pl/oauth/authorize";
                    options.TokenEndpoint = "https://git.kplabs.pl/oauth/token";
                    options.UserInformationEndpoint = "https://git.kplabs.pl/api/v4/user";

                    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "username");
                    options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "name");

                    options.Events = new OAuthEvents
                    {
                        OnCreatingTicket = async context =>
                        {
                            var request =
                                new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            request.Headers.Authorization =
                                new AuthenticationHeaderValue("Bearer", context.AccessToken);

                            var response = await context.Backchannel.SendAsync(request,
                                HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                            response.EnsureSuccessStatusCode();

                            var user = JObject.Parse(await response.Content.ReadAsStringAsync());

                            context.RunClaimActions(user);

                            var userManager = context.HttpContext.RequestServices.GetService(typeof(UserManager<ReviewUser>)) as UserManager<ReviewUser>;
                            
                            var userName = user["username"].Value<string>();
                            var existingUser = await userManager.FindByNameAsync(userName);
                            if (existingUser == null)
                            {   
                                var identityResult = await userManager.CreateAsync(new ReviewUser() {UserName = userName, Token = context.AccessToken});
                                Console.WriteLine($"Create user result: {identityResult.ToString()}");
                            }
                            else
                            {   
                                existingUser.Token = context.AccessToken;
                                await userManager.UpdateAsync(existingUser);
                                Console.WriteLine($"Found existing user with ID {existingUser.Id}, token updated.");
                            }
                        }
                    };
                });


            var builder = new ContainerBuilder();

            builder.Populate(services);

            builder.RegisterInstance(BuildSessionFactory());

            builder.RegisterModule<Cqrs.CqrsModule>();

            builder.Register(BuildGitLabApi).As<IRepository>();

            builder.RegisterType<SignInManager<ReviewUser>>().AsSelf();

            _container = builder.Build();

            return new AutofacServiceProvider(_container);
        }

        private GitLabApi BuildGitLabApi(IComponentContext ctx)
        {
            var cfg = Configuration.GetSection("GitLab");

            return new GitLab.GitLabApi(cfg["url"], cfg["token"]);
        }

        private ISessionFactory BuildSessionFactory()
        {
            var configuration = new NHibernate.Cfg.Configuration();

            configuration.SetProperty(NHibernate.Cfg.Environment.ConnectionString, Configuration.GetConnectionString("Store"));
            configuration.SetProperty(NHibernate.Cfg.Environment.Dialect, typeof(MsSql2012Dialect).AssemblyQualifiedName);

            var modelMapper = new ModelMapper();
            modelMapper.AddMappings(typeof(Startup).Assembly.GetExportedTypes());

            configuration.AddMapping(modelMapper.CompileMappingForAllExplicitlyAddedEntities());

            return configuration.BuildSessionFactory();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (HostingEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = new PathString("/dist"),
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
            });

            var assetServer = Configuration.GetValue<string>("REVIEWER_ASSET_SERVER", null);

            app.UseAuthentication();

            app.Use(async (context, func) =>
            {
                // This is what [Authorize] calls
                var userResult = await context.AuthenticateAsync();
                var user = userResult.Principal;
             
                // Not authenticated
                if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
                {
                    // This is what [Authorize] calls
                    await context.ChallengeAsync();

                    return;
                }
                
                
                await func();
            });

            app.UseOwin(owin => { owin.UseNancy(opt => opt.Bootstrapper = new Bootstraper(assetServer, _container)); });

        }
    }
}