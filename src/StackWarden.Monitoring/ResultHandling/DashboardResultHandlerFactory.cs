﻿using System.Collections.Generic;
using log4net;
using StackWarden.Core.Configuration;
using StackWarden.Monitoring.Configuration;

namespace StackWarden.Monitoring.ResultHandling
{
    public class DashboardResultHandlerFactory : ResultHandlerConfigurationDrivenFactory<DashboardResultHandlerFactory.Configuration, DashboardResultHandler>
    {
        public class Configuration : IConfiguration
        {
            public string Type { get; set; }
            public string HookAddress { get; set; }
        }

        public override IEnumerable<string> SupportedTypeValues => new[] { "Dashboard" };

        public DashboardResultHandlerFactory(string configPath, IConfigurationReader configurationReader) 
            :base(configPath, configurationReader)
        { }

        protected override IEnumerable<DashboardResultHandler> BuildFromConfig(Configuration config)
        {
            yield return new DashboardResultHandler(LogManager.GetLogger(typeof(DashboardResultHandler)), config.HookAddress);
        }
    }
}