﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Threading;
using Lib;
using System.Net;

namespace Client
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            Config.ParallelRequests = 4;
            HttpWebRequest r = (HttpWebRequest)HttpWebRequest.Create("http://www.google.fr");
            Config.Timeout = r.Timeout;
            Config.ReadWriteTimeout = r.ReadWriteTimeout;
            Config.Timeout = 500;

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            Thread listenerThread = new Thread(new ThreadStart(ListenForTcpMessages));
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }

        ListenerSocket listener;
        void ListenForTcpMessages()
        {
            listener = new ListenerSocket("127.0.0.1", 4999, new ConfigMessagesDispatcher());
            listener.Listen();
        }

        protected void Application_End()
        {
            listener.Dispose();
        }
    }
}