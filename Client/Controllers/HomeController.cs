using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading;

namespace Client.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Welcome to ASP.NET MVC!";

            return View();
        }

        public ActionResult Config()
        {
            int io, worker;
            ThreadPool.GetAvailableThreads(out worker, out io);

            ViewBag.IO = io;
            ViewBag.worker = worker;
            ViewBag.requestsread = TestController.RequestsRead;
            ViewBag.asyncoperations = TestController.AsyncOperations;

            return View();
        }

        public ActionResult About()
        {
            return View();
        }
    }
}
