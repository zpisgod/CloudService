﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CloudService.Service.WorkTask
{
    public class WorkTaskSettings
    {
        public int WorkerCount { get; set; }
        public int Interval { get; set; }
    }
}
