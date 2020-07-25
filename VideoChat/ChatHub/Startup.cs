using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChatHub
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR().AddHubOptions<TextChatHub>(config => config.EnableDetailedErrors = true);
            services.AddSignalR().AddHubOptions<VideoChatHub>(config => config.EnableDetailedErrors = true);
            services.AddSignalR(conf =>
            {
                conf.MaximumReceiveMessageSize = null;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<TextChatHub>("/text");
                endpoints.MapHub<VideoChatHub>("/video");
            });
        }
    }
}
