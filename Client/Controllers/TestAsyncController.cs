using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.IO;
using System.Text;
using Lib;

namespace Server.Controllers
{
    public class TestAsyncController : AsyncController
    {
        public static int AsyncOperations = 0;
        public static int RequestsRead = 0;

        //
        // GET: /Async/
        [AsyncTimeout(100000)]
        public void IndexAsync()
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
                AsyncManager.OutstandingOperations.Increment();
                Interlocked.Increment(ref AsyncOperations);
                Interlocked.Increment(ref RequestsRead);
                request.BeginGetResponse(Callback, request);
            });
        }

        protected void Callback(IAsyncResult result)
        {
            Interlocked.Decrement(ref AsyncOperations);
            HttpWebRequest wr = result.AsyncState as HttpWebRequest;

            WebResponse resp = wr.EndGetResponse(result);

            Stream s = resp.GetResponseStream();

            byte[] buf = new byte[1024];
            string sresp = string.Empty;

            while (s.Read(buf, 0, buf.Length) > 0)
            {
                string a = Encoding.ASCII.GetString(buf);
                Console.Write(a);
                sresp += a;
            }

            s.Close();
            s.Dispose();

            AsyncManager.OutstandingOperations.Decrement();
            Interlocked.Decrement(ref RequestsRead);

            if (AsyncManager.OutstandingOperations.Count == 0)
            {
                Response.Flush();
                Response.End();
                Response.Close();
            }

        }

        public ActionResult IndexCompleted()
        {
            return View();
        }
    }
}
