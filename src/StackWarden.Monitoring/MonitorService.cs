﻿using System.Collections.Generic;
using System.Linq;
using log4net;
using StackWarden.Core.Extensions;
using StackWarden.Core.Persistence;
using StackWarden.Monitoring.Configuration;

namespace StackWarden.Monitoring
{
    public class MonitorService : IMonitorService
    {
        private readonly ILog _log;
        private readonly IRepository _repository;
        private readonly IMonitorFactory _monitorFactory;

        public MonitorService(ILog log, IRepository repository, IMonitorFactory monitorFactory)
        {
            _log = log.ThrowIfNull(nameof(log));
            _repository = repository.ThrowIfNull(nameof(repository));
            _monitorFactory = monitorFactory.ThrowIfNull(nameof(monitorFactory));
        }

        public IEnumerable<IMonitor> GetAllMonitors()
        {
            var monitors = _monitorFactory.Build();

            return monitors;
        }

        public IEnumerable<Result> GetLatestResults()
        {
            var latestResults = _repository.Query<Result>()
                                           .GroupBy(x => x.Source != null ? x.Source.Name : null)
                                           .Select(x => x.FirstOrDefault())
                                           .Where(x => x != null);

            return latestResults;
        }

        public void Save(Result result)
        {
            _repository.Save(result);
        }
    }
}