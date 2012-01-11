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
                if (!string.IsNullOrEmpty(Request["time"]))
                    requestUrl += "?time=" + Request["time"];
                HttpWebRequest request = HttpWebRequest.Create(requestUrl) as HttpWebRequest;
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
