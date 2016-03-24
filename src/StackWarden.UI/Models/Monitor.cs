﻿using System;
using System.Collections.Generic;
using System.Linq;
using StackWarden.Monitoring;

namespace StackWarden.UI.Models
{
    public class Monitor
    {
        public string Name { get; set; }
        public string TargetName { get; set; }
        public string State { get; set; }
        public string Message { get; set; }
        public string Icon { get; set; }
        public List<string> Tags { get; set; }
        public Dictionary<string, string> Details { get; set; }
        public DateTime StaleAfter { get; set; }
        public List<Tool> Tools { get; set; }

        public static implicit operator Monitor(MonitorResult result)
        {
            var lenientTimeToLive = result.TimeToLive + Constants.Monitor.TimeToLiveLeniency;
            var projectedResultLife = result.SentOn.AddMilliseconds(lenientTimeToLive);

            var model = new Monitor
            {
                Name = result.SourceName,
                TargetName = result.TargetName,
                State = result.TargetState.ToString(),
                Message = result.FriendlyMessage,
                StaleAfter = projectedResultLife,
                Icon = result.Tags.Where(x => Constants.Icons.Map.ContainsKey(x))
                                  .Select(x => Constants.Icons.Map[x])
                                  .FirstOrDefault(),
                Tags = result.Tags.ToList()
            };

            if (result.Details != null)
                model.Details = new Dictionary<string, string>(result.Details);
            
            return model;
        }
    }
}