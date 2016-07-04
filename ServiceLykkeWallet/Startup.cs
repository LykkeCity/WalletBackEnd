using System.Web.Http;
using Microsoft.Owin.Cors;
using Owin;
using LykkeWalletServices.Transactions.TaskHandlers;

namespace ServiceLykkeWallet
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);

            var config = new HttpConfiguration();
            
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{action}",
                defaults: new { id = RouteParameter.Optional }
            );

            // ToDo - Make the setting reading once in Program.cs
            var settingsTask = SettingsReader.ReadAppSettins();
            settingsTask.Wait();
            var settings = settingsTask.Result;
            config.Properties["assets"] = settings.AssetDefinitions;

            app.UseWebApi(config);
        }
    }
}
