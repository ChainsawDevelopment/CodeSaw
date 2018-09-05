using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using CodeSaw.GitLab;
using CodeSaw.GitLab.Hooks;
using CodeSaw.RepositoryApi;
using CodeSaw.RepositoryApi.Hooks;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Commands.PublishElements;
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
using Web.NodeIntegration;

namespace CodeSaw.Web
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
                    options.DefaultChallengeScheme = "GitLab";
                })
                .AddCookie()
                .AddOAuth("GitLab", options =>
                {
                    ConfigureGitLabOAuth(options);

                    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "username");
                    options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "name");
                    
                    options.Events = new OAuthEvents
                    {
                        OnCreatingTicket = async context => { await HandleCreatingTicket(context); }
                    };
                });

            services.AddMemoryCache();

            var builder = new ContainerBuilder();

            builder.Populate(services);

            builder.RegisterInstance(BuildSessionFactory());

            builder.RegisterModule<Cqrs.CqrsModule>();

            builder.Register(BuildGitLabApi).As<IRepository>().InstancePerLifetimeScope();

            builder.RegisterType<HookHandler>().Named<IHookHandler>("gitlab");
            builder.RegisterType<Modules.Hooks.ReactToHook>().AsImplementedInterfaces();

            builder.RegisterType<SignInManager<ReviewUser>>().AsSelf();
            builder.RegisterType<HttpContextAccessor>().As<IHttpContextAccessor>();

            builder.RegisterType<RevisionFactory>().AsSelf();

            builder.Register(ctx => Configuration.GetValue<string>("HookSiteBase", null) ?? ctx.ResolveKeyed<string>("SiteBase")).Keyed<string>("HookSiteBase");

            ConfigureNodeIntegration(builder);

            _container = builder.Build();

            return new AutofacServiceProvider(_container);
        }

        private void ConfigureNodeIntegration(ContainerBuilder builder)
        {
            var nodeWorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "node-integration");
            var cfg = Configuration.GetSection("Node");

            var executor = new NodeExecutor(cfg["node"], cfg["npm"], nodeWorkingDirectory);

            executor.Bootstrap();

            builder.RegisterInstance(executor).SingleInstance();
        }

        private void ConfigureGitLabOAuth(OAuthOptions options)
        {
            var cfg = Configuration.GetSection("GitLab");

            options.ClientId = cfg["clientId"]; 
            options.ClientSecret = cfg["clientSecret"]; 
            options.CallbackPath = new PathString(cfg["callbackPath"]);

            options.AuthorizationEndpoint = $"{cfg["url"]}/oauth/authorize";
            options.TokenEndpoint = $"{cfg["url"]}/oauth/token";
            options.UserInformationEndpoint = $"{cfg["url"]}/api/v4/user";
        }

        private static async Task HandleCreatingTicket(OAuthCreatingTicketContext context)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

            var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
            response.EnsureSuccessStatusCode();

            var user = JObject.Parse(await response.Content.ReadAsStringAsync());
            var userName = user["username"].Value<string>();
            var givenName = user["name"].Value<string>();
            var avatarUrl = user["avatar_url"].Value<string>();
            if (string.IsNullOrEmpty(avatarUrl))
            {
                avatarUrl = Gravatar.HashEmail(user["email"].Value<string>());
            }

            context.RunClaimActions(user);
                        
            var commandDispatcher = context.HttpContext.RequestServices.GetService(typeof(ICommandDispatcher)) as ICommandDispatcher;
            await commandDispatcher.Execute(new UserTicketCreated(userName, givenName, avatarUrl, context.AccessToken));
        }

        private GitLabApi BuildGitLabApi(IComponentContext ctx)
        {
            var cfg = Configuration.GetSection("GitLab");

            return new GitLabApi(cfg["url"], ctx.Resolve<IGitAccessTokenSource>());
        }

        private ISessionFactory BuildSessionFactory()
        {
            var configuration = new NHibernate.Cfg.Configuration();

            configuration.SetProperty(NHibernate.Cfg.Environment.ConnectionString, Configuration.GetConnectionString("Store"));
            configuration.SetProperty(NHibernate.Cfg.Environment.Dialect, typeof(MsSql2012Dialect).AssemblyQualifiedName);

            var modelMapper = new ModelMapper();
            modelMapper.AddMappings(typeof(Startup).Assembly.GetExportedTypes());

            var hbm = modelMapper.CompileMappingForAllExplicitlyAddedEntities();
            
            configuration.AddMapping(hbm);

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
            var globalToken = Configuration.GetSection("GitLab").GetValue<string>("globalToken");

            app.UseAuthentication();

            app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/hooks"), sub => sub.ChallengeAllUnauthenticatedCalls());

            app.UseOwin(owin => { owin.UseNancy(opt => opt.Bootstrapper = new Bootstraper(assetServer, _container, globalToken)); });
        }
    }
}