using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Web.Configuration;
using System.Configuration;

namespace ConfigUpdate
{
    class Program
    {
        static void Main(string[] args)
        {
            Configuration machineConf = WebConfigurationManager.OpenMachineConfiguration();

            ConfigurationSectionGroup systemWeb = machineConf.GetSectionGroup("system.web");
            ConfigurationSection processModelS = systemWeb.Sections.Get("processModel");
            ProcessModelSection processModel = processModelS as ProcessModelSection;
            if (processModel != null)
            {
                processModel.MinIOThreads = 1;
                machineConf.Save();
            }

        }
    }
}
