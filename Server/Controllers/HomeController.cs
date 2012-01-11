using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Threading;

namespace Server.Controllers
{
    public class HomeController : Controller
    {
        const int bufferSize = 256;
        public ActionResult Index()
        {
            int size = 1;

            byte[] buf = new byte[bufferSize];
            if (!string.IsNullOrEmpty(Request["size"]))
                Int32.TryParse(Request["size"], out size);

            HttpContext.Response.Buffer = false;
            
            string time =  Request["time"];
            int sleepTime=0;
            Int32.TryParse(time, out sleepTime);// && sleepTime > 0)

            int sent = 0;
            while (sent < size * 1024)
            {
                for (int i = 0; i < bufferSize; i++)
                    buf[i] = 0x41;
                HttpContext.Response.OutputStream.Write(buf, 0, buf.Length);
                HttpContext.Response.OutputStream.Flush();
                Thread.Sleep(sleepTime);
                sent += bufferSize;
            }

                //Thread.Sleep(sleepTime);


            HttpContext.Response.Flush();
            HttpContext.Response.End();

            return View();
        }

        public ActionResult About()
        {
            return View();
        }
    }
}
