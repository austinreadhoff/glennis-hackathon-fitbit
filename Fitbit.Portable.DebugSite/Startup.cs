using Microsoft.Owin;
using Owin;
using System.Net;

[assembly: OwinStartupAttribute(typeof(SampleWebMVCOAuth2.Startup))]
namespace SampleWebMVCOAuth2
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);


            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13
                                    | SecurityProtocolType.Tls12
                                     ;

        }
    }
}
