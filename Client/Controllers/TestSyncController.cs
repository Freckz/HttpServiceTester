using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Net;
using Lib;
using System.IO;
using System.Text;

namespace Server.Controllers
{
    public class TestSyncController : Controller
    {
        //
        // GET: /Sync/

        public ActionResult Index()
        {
            Parallel.For(0, Config.ParallelRequests, i =>
            {
                string requestUrl = "http://Server.loc";
                bool withTime = false;
                if (!string.IsNullOrEmpty(Request["time"]))
                {
                    requestUrl += "?time=" + Request["time"];
                    withTime = true;
                }
                if (!string.IsNullOrEmpty(Request["size"]))
                    requestUrl += string.Format("{0}size={1}", withTime ? "&" : "?", Request["size"]);
                HttpWebRequest request = HttpWebRequest.Create(requestUrl) as HttpWebRequest;
                request.ReadWriteTimeout = Config.ReadWriteTimeout;
                request.Timeout = Config.Timeout;
                WebResponse response = request.GetResponse();
                
                Stream s = response.GetResponseStream();
                byte[] buffer = new byte[256];
                while (s.Read(buffer, 0, 256) > 0)
                    Console.WriteLine(Encoding.ASCII.GetString(buffer));
            });

            return View();
        }

    }
}
