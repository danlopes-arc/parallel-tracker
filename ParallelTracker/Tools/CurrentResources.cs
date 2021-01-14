using Microsoft.AspNetCore.Http;
using ParallelTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelTracker.Tools
{
    public class CurrentResources
    {
        public Repo Repo { get; set; }
        public Issue Issue { get; set; }
    }
}
