using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MXH_HIOTFZone
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Kích hoạt SignalR
            app.MapSignalR();
        }
    }
}