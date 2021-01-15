using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelTracker.Models
{
    public class User : IdentityUser
    {
        public string Name { get; set; }

        public IEnumerable<Repo> Repos { get; set; }
        public IEnumerable<Issue> Issues { get; set; }
        public IEnumerable<Comment> Comments { get; set; }
    }
}
