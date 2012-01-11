using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Threading;

namespace Client.Controllers
{
    public class TestController : AsyncController
    {
        public static int AsyncOperations = 0;
        public static int RequestsRead = 0;

        //
        // GET: /Async/
        [AsyncTimeout(100000)]
        public void IndexAsync()
        {
            Parallel.For(0, 11000, i =>
            {
                HttpWebRequest request = HttpWebRequest.Create("http://server.test/") as HttpWebRequest;
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

            while (s.Read(buf, 0, buf.Length)>0)
            {
                string a = Encoding.ASCII.GetString(buf);
                Console.Write(a);
                sresp += a;
            }

            s.Close();
            s.Dispose();

            AsyncManager.OutstandingOperations.Decrement();
            Interlocked.Decrement(ref RequestsRead);
        }

        public ActionResult IndexCompleted()
        {
            return View();
        }
    }
}
