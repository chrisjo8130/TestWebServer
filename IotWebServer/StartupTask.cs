using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace IotWebServer
{
    public sealed class StartupTask : IBackgroundTask
    {
        HttpWebServer server;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            HtmlDoc htmlDoc = new HtmlDoc();
            string reply = htmlDoc.ResponseText;
            server = new HttpWebServer(reply);
            server.Initialise();

            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //
        }
    }
}
