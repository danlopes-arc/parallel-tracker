using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParallelTracker.Data;
using ParallelTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace ParallelTracker.Controllers
{
    public class ResetDataController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public ResetDataController(ApplicationContext context, UserManager<User> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var r = new Random();

            var users = new[]
            {
                new User
                {
                    UserName = "betty",
                    Email = "betty@paralleltracker.com",
                },
                new User
                {
                    UserName = "rose",
                    Email = "rose@paralleltracker.com",
                },
                new User
                {
                    UserName = "codemaster",
                    Email = "codemaster@paralleltracker.com",
                }
            };

            var repos = new List<Repo>();

            foreach (var name in RepoNames)
            {
                try
                {
                    using var repoClient = CreateClient($"/repos/{name}");
                    var repoDto = await repoClient.GetFromJsonAsync<Repo.RepoDeserialized>("");
                    var repo = new Repo
                    {
                        Owner = users[r.Next(1, users.Length)],
                        AvatarUrl = repoDto.owner.avatar_url,
                        CopiedAt = DateTime.Now.AddSeconds(r.Next(-2592000, 0)),
                        Description = repoDto.description,
                        CreatedAt = repoDto.created_at,
                        FullName = repoDto.full_name,
                        GitHubOwnerLogin = repoDto.owner.login,
                        GitHubOwnerUrl = repoDto.owner.html_url,
                        Name = repoDto.name,
                        Url = repoDto.html_url
                    };
                    repos.Add(repo);

                    try
                    {
                        using var issueClient = CreateClient($"/repos/{name}/issues?per_page={r.Next(0, 20)}");
                        var issueDtos = await issueClient.GetFromJsonAsync<IEnumerable<IssueDto>>("");
                        var issues = new List<Issue>();
                        foreach (var issueDto in issueDtos)
                        {
                            var issue = new Issue
                            {
                                Author = users[r.Next(1, users.Length)],
                                CreatedAt = repo.CreatedAt.AddSeconds(r.Next(0, (int)(DateTime.Now - repo.CreatedAt).TotalSeconds)),
                                //RepoId = repo.Id,
                                Repo = repo,
                                Text = issueDto.body,
                                Title = issueDto.title
                            };
                            if (r.Next(10) < 3) // is closed?
                            {
                                issue.ClosedAt = issue.CreatedAt.AddSeconds(r.Next(0, (int)(DateTime.Now - issue.CreatedAt).TotalSeconds));
                            }
                            if (r.Next(10) < 2) // is edited?
                            {
                                issue.EditedAt = issue.CreatedAt.AddSeconds(r.Next(0, (int)(DateTime.Now - issue.CreatedAt).TotalSeconds));
                            }

                            issues.Add(issue);

                            try
                            {
                                using var commentClient = CreateClient($"/repos/{name}/issues/{issueDto.number}/comments?per_page={r.Next(0, 20)}");
                                var commentDtos = await commentClient.GetFromJsonAsync<IEnumerable<CommnentDto>>("");
                                var comments = new List<Comment>();
                                foreach (var commentDto in commentDtos)
                                {
                                    var comment = new Comment
                                    {
                                        Author = users[r.Next(1, users.Length)],
                                        CreatedAt = issue.CreatedAt.AddSeconds(r.Next(0, (int)(DateTime.Now - issue.CreatedAt).TotalSeconds)),
                                        Issue = issue,
                                        Text = commentDto.body
                                    };
                                    if (r.Next(10) < 1) // is edited?
                                    {
                                        issue.EditedAt = comment.CreatedAt.AddSeconds(r.Next(0, (int)(DateTime.Now - comment.CreatedAt).TotalSeconds));
                                    }
                                    comments.Add(comment);
                                }
                                issue.Comments = comments;
                            }
                            catch (Exception)
                            {
                                return View(new StringBuilder($"Something happend when trying to fetch {name} issue {issueDto.number} comments"));
                            }
                        }
                        repo.Issues = issues;
                    }
                    catch (Exception)
                    {

                        return View(new StringBuilder($"Something happend when trying to fetch {name} issues"));
                    }
                }
                catch (Exception)
                {
                    return View(new StringBuilder($"Something happend when trying to fetch {name}"));
                }
            }

            await _context.Database.ExecuteSqlRawAsync(
                @"DO $$ DECLARE
                    r RECORD;
                BEGIN
                    FOR r IN(SELECT tablename FROM pg_tables WHERE schemaname = current_schema()) LOOP
                        EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
                    END LOOP;
                END $$; ");

            await _context.Database.MigrateAsync();
            foreach (var user in users)
            {
                var result = await _userManager.CreateAsync(user, $"!aA{r.Next(10000000, 99999999)}");
            }

            try
            {
                _context.AddRange(repos);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return View(new StringBuilder("Error trying to save to the database"));
            }

            return View(new StringBuilder("Done"));
        }

        private HttpClient CreateClient(string relativePath)
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri($"https://api.github.com{relativePath}")
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("PrarallelTracker", "1.0"));
            var configSection = _configuration.GetSection("GitHubApi");
            var byteArray = Encoding.ASCII.GetBytes($"{configSection["ClientId"]}:{configSection["ClientSecret"]}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            return client;
        }

        private readonly string[] RepoNames = new[]
        {
            "dotnet/aspnetcore",
            "dotnet/runtime",
            "nodejs/node",
            "microsoft/TypeScript",
            "postgres/postgres",
            "vercel/next.js",
            "vuejs/vue",
            "facebook/react",
            "angular/angular",
            "redis/redis",
            "mongodb/mongo",
            "graphql-go/graphql",
            "microsoft/WSL",
            "preactjs/preact",
            "Automattic/mongoose"
        };

        private class IssueDto
        {
            public string url { get; set; }
            public string repository_url { get; set; }
            public string labels_url { get; set; }
            public string comments_url { get; set; }
            public string events_url { get; set; }
            public string html_url { get; set; }
            public int id { get; set; }
            public string node_id { get; set; }
            public int number { get; set; }
            public string title { get; set; }
            public GitHubUser user { get; set; }
            public Label[] labels { get; set; }
            public string state { get; set; }
            public bool locked { get; set; }
            public Assignee assignee { get; set; }
            public Assignee[] assignees { get; set; }
            public Milestone milestone { get; set; }
            public int comments { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public object closed_at { get; set; }
            public string author_association { get; set; }
            public object active_lock_reason { get; set; }
            public string body { get; set; }
            public object performed_via_github_app { get; set; }
            public Pull_Request pull_request { get; set; }
        }

        private class GitHubUser
        {
            public string login { get; set; }
            public int id { get; set; }
            public string node_id { get; set; }
            public string avatar_url { get; set; }
            public string gravatar_id { get; set; }
            public string url { get; set; }
            public string html_url { get; set; }
            public string followers_url { get; set; }
            public string following_url { get; set; }
            public string gists_url { get; set; }
            public string starred_url { get; set; }
            public string subscriptions_url { get; set; }
            public string organizations_url { get; set; }
            public string repos_url { get; set; }
            public string events_url { get; set; }
            public string received_events_url { get; set; }
            public string type { get; set; }
            public bool site_admin { get; set; }
        }

        private class Assignee
        {
            public string login { get; set; }
            public int id { get; set; }
            public string node_id { get; set; }
            public string avatar_url { get; set; }
            public string gravatar_id { get; set; }
            public string url { get; set; }
            public string html_url { get; set; }
            public string followers_url { get; set; }
            public string following_url { get; set; }
            public string gists_url { get; set; }
            public string starred_url { get; set; }
            public string subscriptions_url { get; set; }
            public string organizations_url { get; set; }
            public string repos_url { get; set; }
            public string events_url { get; set; }
            public string received_events_url { get; set; }
            public string type { get; set; }
            public bool site_admin { get; set; }
        }

        private class Milestone
        {
            public string url { get; set; }
            public string html_url { get; set; }
            public string labels_url { get; set; }
            public int id { get; set; }
            public string node_id { get; set; }
            public int number { get; set; }
            public string title { get; set; }
            public string description { get; set; }
            public Creator creator { get; set; }
            public int open_issues { get; set; }
            public int closed_issues { get; set; }
            public string state { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public DateTime? due_on { get; set; }
            public object closed_at { get; set; }
        }

        private class Creator
        {
            public string login { get; set; }
            public int id { get; set; }
            public string node_id { get; set; }
            public string avatar_url { get; set; }
            public string gravatar_id { get; set; }
            public string url { get; set; }
            public string html_url { get; set; }
            public string followers_url { get; set; }
            public string following_url { get; set; }
            public string gists_url { get; set; }
            public string starred_url { get; set; }
            public string subscriptions_url { get; set; }
            public string organizations_url { get; set; }
            public string repos_url { get; set; }
            public string events_url { get; set; }
            public string received_events_url { get; set; }
            public string type { get; set; }
            public bool site_admin { get; set; }
        }

        private class Pull_Request
        {
            public string url { get; set; }
            public string html_url { get; set; }
            public string diff_url { get; set; }
            public string patch_url { get; set; }
        }

        private class Label
        {
            public long id { get; set; }
            public string node_id { get; set; }
            public string url { get; set; }
            public string name { get; set; }
            public string color { get; set; }
            public bool _default { get; set; }
            public string description { get; set; }
        }


        private class CommnentDto
        {
            public string url { get; set; }
            public string html_url { get; set; }
            public string issue_url { get; set; }
            public int id { get; set; }
            public string node_id { get; set; }
            public GitHubUser user { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public string author_association { get; set; }
            public string body { get; set; }
            public object performed_via_github_app { get; set; }
        }
    }
}
