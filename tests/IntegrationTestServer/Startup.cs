using GraphQL;
using GraphQL.Client.Tests.Common;
using GraphQL.Client.Tests.Common.Chat.Schema;
using GraphQL.Client.Tests.Common.StarWars;
using GraphQL.Server;
using GraphQL.Server.Ui.Altair;
using GraphQL.Server.Ui.GraphiQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IntegrationTestServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<KestrelServerOptions>(options => options.AllowSynchronousIO = true);
            //
            services.AddChatSchema();
            services.AddStarWarsSchema();
            services.AddSingleton<IDocumentExecuter, SubscriptionDocumentExecuter>();
            services.AddGraphQL((options, services) =>
            {
                options.EnableMetrics = true;
                var logger = services.GetRequiredService<ILogger<Startup>>();
                options.UnhandledExceptionDelegate = ctx => logger.LogError("{Error} occurred", ctx.OriginalException.Message);
            })
                .AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = Environment.IsDevelopment())
                .AddSystemTextJson()
                .AddWebSockets()
                .AddGraphTypes(typeof(ChatSchema))
                .AddGraphTypes(typeof(StarWarsSchema));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWebSockets();

            ConfigureGraphQLSchema<ChatSchema>(app, Common.CHAT_ENDPOINT);
            ConfigureGraphQLSchema<StarWarsSchema>(app, Common.STAR_WARS_ENDPOINT);

            app.UseGraphQLGraphiQL(new GraphiQLOptions { GraphQLEndPoint = Common.STAR_WARS_ENDPOINT });
            app.UseGraphQLAltair(new AltairOptions { GraphQLEndPoint = Common.CHAT_ENDPOINT });
        }

        private void ConfigureGraphQLSchema<TSchema>(IApplicationBuilder app, string endpoint) where TSchema : Schema
        {
            app.UseGraphQLWebSockets<TSchema>(endpoint);
            app.UseGraphQL<TSchema>(endpoint);
        }
    }
}
