﻿using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using log4net;
using StackWarden.Core;
using StackWarden.Core.Configuration;
using StackWarden.Core.Extensions;

namespace StackWarden.Monitoring.Machine
{
    public class PerformanceMonitor : Monitor
    {
        private readonly string _machineName;

        public Dictionary<SeverityState, int> CpuUsageSeverity => new Dictionary<SeverityState, int>
        {
            { SeverityState.Warning, 80 },
            { SeverityState.Error, 90 }
        };

        public Dictionary<SeverityState, int> MemoryAvailableSeverity => new Dictionary<SeverityState, int>
        {
            { SeverityState.Warning, 1500 },
            { SeverityState.Error, 500 }
        };

        public Dictionary<SeverityState, int> DiskSpaceAvailableSeverity = new Dictionary<SeverityState, int>
        {
            { SeverityState.Warning, 20 },
            { SeverityState.Error, 5 }
        };

        public Dictionary<SeverityState, int> MSMQStorageUsageSeverity = new Dictionary<SeverityState, int>
        {
            { SeverityState.Warning, 1400 },
            { SeverityState.Error, 1600 }
        };

        public PerformanceMonitor(ILog log, string machineName)
            :base(log, $"Performance monitor for {machineName}.")
        {
            _machineName = machineName.ThrowIfNullOrWhiteSpace(nameof(machineName));
        }

        protected override void Update(MonitorResult result)
        {
            CheckCpu(result);
            CheckMemory(result);
            CheckDiskSpace(result);
            CheckMSMQStorage(result);
        }

        private SeverityState GetThresholdState<T>(SeverityState resultState, Dictionary<SeverityState, T> criteriaSource, Func<T, bool> compare)
        {
            if (resultState == SeverityState.Error)
                return resultState;

            foreach (var currentState in Enum.GetValues(typeof(SeverityState)).Cast<SeverityState>())
                if (criteriaSource.ContainsKey(currentState) && compare(criteriaSource[currentState]))
                    return currentState;

            return SeverityState.Normal;
        }

        private void AddDetail<T>(MonitorResult result,
                                  Func<PerformanceCounter> buildCounter,
                                  string name,
                                  string valueFormatString,
                                  Dictionary<SeverityState, T> thresholdCriteria,
                                  Func<T, float, bool> compareThreshold)
        {
            AddDetail(result, buildCounter, name, valueFormatString, x => x, thresholdCriteria, compareThreshold);
        }

        private void AddDetail<T>(MonitorResult result,
                                  Func<PerformanceCounter> buildCounter,
                                  string name,
                                  string valueFormatString,
                                  Func<float, float> transformValue,
                                  Dictionary<SeverityState, T> thresholdCriteria,
                                  Func<T, float, bool> compareThreshold)
        {
            float? counterValue = null;

            try
            {
                var counter = buildCounter();
                counterValue = counter.NextValue();

                if (transformValue != null && counterValue.HasValue)
                    counterValue = transformValue(counterValue.Value);
            }
            catch (Exception) { /* Using N/A for failure output. */ }

            result.Details.Add(name, string.Format(valueFormatString, counterValue?.ToString() ?? "N/A"));

            if (!counterValue.HasValue)
                return;

            result.TargetState = GetThresholdState(result.TargetState,
                                                   thresholdCriteria,
                                                   threshold => compareThreshold(threshold, counterValue.Value));
        }

        private void CheckCpu(MonitorResult result)
        {
            AddDetail(result,
                      () => new PerformanceCounter
                      {
                          MachineName = _machineName,
                          CategoryName = "Processor",
                          CounterName = "% Processor Time",
                          InstanceName = "_Total"
                      },
                      "CPU Usage",
                      "{0}%",
                      CpuUsageSeverity,
                      (threshold, cpuUsage) => threshold >= cpuUsage);
        }

        private void CheckMemory(MonitorResult result)
        {
            AddDetail(result,
                      () => new PerformanceCounter
                      {
                          MachineName = _machineName,
                          CategoryName = "Memory",
                          CounterName = "Available MBytes"
                      },
                      "Memory Available",
                      "{0}MB",
                      MemoryAvailableSeverity,
                      (threshold, memoryAvailable) => threshold <= memoryAvailable);
        }

        private void CheckDiskSpace(MonitorResult result)
        {
            AddDetail(result,
                      () => new PerformanceCounter
                      {
                          MachineName = _machineName,
                          CategoryName = "LogicalDisk",
                          CounterName = "% Free Space"
                      },
                      "Disk Space Available",
                      "{0}%",
                      DiskSpaceAvailableSeverity,
                      (threshold, diskAvailable) => threshold <= diskAvailable);
        }
        
        private void CheckMSMQStorage(MonitorResult result)
        {
            AddDetail(result,
                      () => new PerformanceCounter
                      {
                          MachineName = _machineName,
                          CategoryName = "MSMQ Service",
                          CounterName = "Total bytes in all queues"
                      },
                      "MSMQ Storage Usage",
                      "{0}MB",
                      value => value / Constants.Units.Kibi / Constants.Units.Kibi,
                      MSMQStorageUsageSeverity,
                      (threshold, storageUsage) => threshold >= storageUsage);
        }
    }
}