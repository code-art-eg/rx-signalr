using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;
using SignalRTestApp;
using System.Diagnostics;

[assembly: OwinStartup(typeof(Startup))]

namespace SignalRTestApp
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
