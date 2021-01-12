using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelTracker.Models
{
    public class Repo
    {
        public Repo()
        {

        }

        public Repo(RepoDeserialized repo)
        {
            Url = repo.html_url;
            Name = repo.name;
            FullName = repo.full_name;
            AvatarUrl = repo.owner.avatar_url;
            Description = repo.description;
            CreatedAt = repo.created_at;
            GitHubOwnerLogin = repo.owner.login;
            GitHubOwnerUrl = repo.owner.html_url;
        }

        public int Id { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ParalleledAt { get; set; }
        public string GitHubOwnerLogin { get; set; }
        public string GitHubOwnerUrl { get; set; }
        public string OwnerId { get; set; }

        public User Owner { get; set; }


        public class RepoDeserialized
        {
            public string name { get; set; }
            public string full_name { get; set; }
            public string html_url { get; set; }
            public string description { get; set; }
            public DateTime created_at { get; set; }
            public OwnerDeserialized owner { get; set; }
        }

        public class OwnerDeserialized
        {
            public string login { get; set; }
            public string avatar_url { get; set; }
            public string html_url { get; set; }
        }

    }
}
