using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;
using SignalRTestSite.App_Start;

[assembly: OwinStartup(typeof(Startup))]

namespace SignalRTestSite.App_Start
{
  public class Startup
  {
    public static void Configuration(IAppBuilder app)
    {
      app.UseCors(CorsOptions.AllowAll);
      app.MapSignalR();
      app.UseWelcomePage();
    }
  }
}
