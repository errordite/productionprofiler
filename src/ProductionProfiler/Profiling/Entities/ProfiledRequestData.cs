﻿using System;
using System.Collections.Generic;

namespace ProductionProfiler.Core.Profiling.Entities
{
    [Serializable]
    public class ProfiledRequestData
    {
        public Guid Id { get; set; }
        public DateTime CapturedOnUtc { get; set; }
        public string Url { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public string Server { get; set; }
        public string ClientIpAddress { get; set; }
        public string UserAgent { get; set; }
        public bool Ajax { get; set; }
        public List<ProfiledMethodData> Methods { get; set; }
        public List<ProfilerError> ProfilerErrors { get; set; }
        public List<DataCollection> CollectedData { get; set; }

        public ProfiledRequestData()
        {
            CollectedData = new List<DataCollection>();
            Methods = new List<ProfiledMethodData>();
        }
    }
}