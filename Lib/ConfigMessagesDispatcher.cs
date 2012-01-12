using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Net.Configuration;
using System.Web;
using System.Threading;
using System.Web.Configuration;
using System.Reflection;

namespace Lib
{
    public class ConfigMessagesDispatcher : ISocketMessageDispatcher
    {
        int DefaultMaxConnection
        {
            get
            {
                return System.Net.ServicePointManager.DefaultConnectionLimit;
            }
        }

        public string Action(string message)
        {
            string[] args = message.Split('|');
            string response = string.Empty;

            foreach (string s in args)
            {
                string[] subargs = s.Split(':');
                switch (subargs[0])
                {
                    case "GET":
                        response += GetResponse();
                        break;
                    case "SET":
                        Set(subargs);
                        response += GetResponse();
                        break;
                    case "PING":
                        response += "PONG";
                        break;
                    default:
                        break;
                }
            }
            return response;
        }

        void Set(string[] conf)
        {
            if (conf.Length < 3)
                return;

            
            try
            {
                ConfigType config = (ConfigType)Enum.Parse(typeof(ConfigType), conf[1]);
                switch (config)
                {
                    case ConfigType.MaxConnections:
                        object connMgt = ConfigurationManager.OpenMachineConfiguration().GetSectionGroup("system.net").Sections["connectionManagement"];
                        ConnectionManagementSection connMgtSection = connMgt as ConnectionManagementSection;
                        connMgtSection.ConnectionManagement.Clear();
                        connMgtSection.ConnectionManagement.Add(new ConnectionManagementElement("*", int.Parse(conf[2])));
                        break;
                    case ConfigType.MaxIOThreads:
                        int iomiot, wmiot;
                        ThreadPool.GetMaxThreads(out wmiot, out iomiot);
                        ThreadPool.SetMaxThreads(wmiot, int.Parse(conf[2]));
                        break;
                    case ConfigType.MaxWorkerThreads:
                        int iomwt, wmwt;
                        ThreadPool.GetMaxThreads(out wmwt, out iomwt);
                        ThreadPool.SetMaxThreads(int.Parse(conf[2]), iomwt);
                        break;
                    case ConfigType.RequestQueueLimit:
                        ProcessModelSection pms = (ProcessModelSection)ConfigurationManager.OpenMachineConfiguration().GetSectionGroup("system.web").Sections["processModel"];
                        pms.RequestQueueLimit = int.Parse(conf[2]);
                        break;
                    case ConfigType.ParallelDistantRequestValue:
                        Config.ParallelRequests = int.Parse(conf[2]);
                        break;
                    case ConfigType.Timeout:
                        Config.Timeout = int.Parse(conf[2]);
                        break;
                    case ConfigType.ReadWriteTimeout:
                        Config.ReadWriteTimeout = int.Parse(conf[2]);
                        break;
                    case ConfigType.AvailableIOThreads:
                    case ConfigType.AvailableWorkerThreads:
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
            }
        }

        string GetResponse()
        {
            Dictionary<string, string> configs = GetConfigs();
            string resp = string.Empty;
            if (configs.Count > 0)
            {
                IEnumerable<string> ies = configs.Select((kvp, s) => string.Concat(kvp.Key, ":", kvp.Value, "|"));
                resp = ies.Aggregate((s, next) => s += next);
                resp = resp.TrimEnd('|');
            }
            return resp;
        }

        Dictionary<string, string> GetConfigs()
        {
            Dictionary<string, string> configs = new Dictionary<string, string>();

            #region Custom configs
            configs.Add("ParallelDistantRequestValue", Config.ParallelRequests.ToString());
            #endregion

            #region ProcessModel
            ProcessModelSection pms = (ProcessModelSection)ConfigurationManager.OpenMachineConfiguration().GetSectionGroup("system.web").Sections["processModel"];
            configs.Add("RequestQueueLimit", pms.RequestQueueLimit.ToString());
            #endregion

            #region HttpRuntime RequestQueue
            HttpRuntime runtime = (HttpRuntime)((FieldInfo)typeof(HttpRuntime).GetField("_theRuntime", BindingFlags.NonPublic | BindingFlags.Static)).GetValue(null);
            object requestQueue = ((FieldInfo)typeof(HttpRuntime).GetField("_requestQueue", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField)).GetValue(runtime);
            Assembly sweb = Assembly.Load(new AssemblyName("System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));
            Type RequestQueue = sweb.GetType("System.Web.RequestQueue");//.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);	//System.Reflection.FieldInfo[]
            configs.Add("runtimeQueueCount", RequestQueue.GetField("_count", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField).GetValue(requestQueue).ToString());
            #endregion

            #region Connection management
            object connMgt = ConfigurationManager.OpenMachineConfiguration().GetSectionGroup("system.net").Sections["connectionManagement"];
            ConnectionManagementSection connMgtSection = connMgt as ConnectionManagementSection;
            int maxConns = 0;
            if (connMgtSection != null && connMgtSection.ConnectionManagement.Count > 0)
                maxConns = connMgtSection.ConnectionManagement[0].MaxConnection;
            else
                maxConns = DefaultMaxConnection;
            configs.Add("maxconnections", maxConns.ToString());
            #endregion

            #region Thread pool available threads
            int wt, iot;
            ThreadPool.GetAvailableThreads(out wt, out iot);
            configs.Add("AvailableWorkerThreads", wt.ToString());
            configs.Add("AvailableIOThreads", iot.ToString());
            #endregion

            #region Thread pool Max threads
            int mwt, miot;
            ThreadPool.GetMaxThreads(out mwt, out miot);
            configs.Add("MaxWorkerThreads", mwt.ToString());
            configs.Add("MaxIOThreads", miot.ToString());
            #endregion

            #region Thread pool Max threads
            int minwt, miniot;
            ThreadPool.GetMinThreads(out minwt, out miniot);
            configs.Add("MinWorkerThreads", minwt.ToString());
            configs.Add("MinIOThreads", miniot.ToString());
            #endregion

            #region Timeout
            configs.Add("Timeout", Config.Timeout.ToString());
            configs.Add("ReadWriteTimeout", Config.ReadWriteTimeout.ToString());
            #endregion

            return configs;
        }
    }
}
