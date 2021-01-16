using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ParallelTracker.Data;
using ParallelTracker.Models;
using System.Net.Http.Headers;
using ParallelTracker.Tools;
using System.Security.Claims;

namespace ParallelTracker.Controllers
{
    [Authorize]
    public class ReposController : Controller
    {
        public const int RepoPageSize = 10;
        public const int IssuePageSize = 15;

        private readonly ApplicationContext _context;

        public ReposController(ApplicationContext context)
        {
            _context = context;
        }

        // GET: Repos
        [AllowAnonymous]
        public async Task<IActionResult> Index([Bind] RepoFilter filter, int? page)
        {
            IEnumerable<Repo> repos = await _context.Repos
                .Include(r => r.Owner)
                .ToListAsync();

            filter.SortMode ??= RepoSortModes.Newest;
            switch (filter.SortMode)
            {
                case RepoSortModes.Oldest:
                    repos = repos.OrderBy(r => r.CopiedAt);
                    break;
                case RepoSortModes.Newest:
                default:
                    repos = repos.OrderByDescending(r => r.CopiedAt);
                    filter.SortMode = RepoSortModes.Newest;
                    break;
            }
            var sortModeSelectList = new SelectList(new[]
            {
                new { Code = RepoSortModes.Newest, Text = "Newest"},
                new { Code = RepoSortModes.Oldest, Text = "Oldest"}
            }, "Code", "Text", filter.SortMode);

            if (!string.IsNullOrEmpty(filter.Text))
            {
                var lowerText = filter.Text.ToLower();

                repos = repos
                    .Where(r => r.FullName.ToLower().Contains(lowerText) ||
                    r.Description.ToLower().Contains(lowerText))
                    .ToList();
            }

            page = Math.Max(page.GetValueOrDefault(), 1);
            var totalPages = (int)Math.Ceiling(repos.Count() / (float)RepoPageSize);

            if (page > 0)
            {
                repos = repos
                    .Skip(RepoPageSize * (page.Value - 1))
                    .Take(RepoPageSize);
            }

            return View(new RepoIndexVm
            {
                Repos = repos,
                Filter = filter,
                SortModeSelectList = sortModeSelectList,
                Page = page.Value,
                TotalPages = totalPages
            });
        }

        // GET: Repos/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id, [Bind] IssueFilter filter, int? page)
        {
            if (id == null)
            {
                return NotFound();
            }

            var repo = await _context.Repos
                .Include(r => r.Owner)
                .Include(r => r.Issues)
                    .ThenInclude(i => i.Author)
                .Include(r => r.Issues)
                    .ThenInclude(i => i.Comments)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (repo == null)
            {
                return NotFound();
            }

            filter.SortMode ??= IssueSortModes.Newest;
            switch (filter.SortMode)
            {
                case IssueSortModes.Oldest:
                    repo.Issues = repo.Issues.OrderBy(i => i.CreatedAt);
                    break;
                case IssueSortModes.MostComments:
                    repo.Issues = repo.Issues.OrderByDescending(i => i.Comments.Count());
                    break;
                case IssueSortModes.LeastComments:
                    repo.Issues = repo.Issues.OrderBy(i => i.Comments.Count());
                    break;
                case IssueSortModes.Newest:
                default:
                    repo.Issues = repo.Issues.OrderByDescending(i => i.CreatedAt);
                    filter.SortMode = IssueSortModes.Newest;
                    break;
            }
            var sortModeSelectList = new SelectList(new[]
            {
                new { Code = IssueSortModes.Newest, Text = "Newest"},
                new { Code = IssueSortModes.Oldest, Text = "Oldest"},
                new { Code = IssueSortModes.MostComments, Text = "Most Comments"},
                new { Code = IssueSortModes.LeastComments, Text = "Least Comments"},
            }, "Code", "Text", filter.SortMode);


            filter.Status ??= IssueStatus.All;
            if (filter.YourIssues && User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                repo.Issues = repo.Issues.Where(i => i.AuthorId == userId);
            }
            switch (filter.Status)
            {
                case IssueStatus.Closed:
                    repo.Issues = repo.Issues.Where(i => i.IsClosed);
                    break;
                case IssueStatus.Open:
                    repo.Issues = repo.Issues.Where(i => !i.IsClosed);
                    break;
                case IssueStatus.All:
                default:
                    filter.Status = IssueStatus.All;
                    break;
            }
            var issueStatusSelectList = new SelectList(new[]
            {
                new { Code = IssueStatus.All, Text = "All Issues"},
                new { Code = IssueStatus.Open, Text = "Open Issues"},
                new { Code = IssueStatus.Closed, Text = "Closed Issues"},
            }, "Code", "Text", filter.Status);

            if (filter.YourIssues && User.Identity.IsAuthenticated)
            {
                repo.Issues = repo.Issues.Where(r => r.AuthorId == User.FindFirstValue(ClaimTypes.NameIdentifier));
            }

            if (!string.IsNullOrEmpty(filter.Text))
            {
                var lowerText = filter.Text.ToLower();
                await _context.Comments
                    .Where(c => c.Issue.RepoId == repo.Id)
                    .ToListAsync();

                repo.Issues = repo.Issues
                    .Where(i => i.Title.ToLower().Contains(lowerText) ||
                    i.Text.ToLower().Contains(lowerText) ||
                    (i.FindCommentsByText(lowerText)?.Count() ?? 0) > 0);
            }

            page  = Math.Max(page.GetValueOrDefault(), 1);
            var totalPages = (int)Math.Ceiling(repo.Issues.Count() / (float)IssuePageSize);

            if (page > 0)
            {
                repo.Issues = repo.Issues
                    .Skip(IssuePageSize * (page.Value - 1))
                    .Take(IssuePageSize);
            }

            return View(new RepoDetailsVm
            {
                Repo = repo,
                Filter = filter,
                SortModeSelectList = sortModeSelectList,
                IssueStatusSelectList = issueStatusSelectList,
                Page = page.Value,
                TotalPages = totalPages
            });
        }

        // GET: Repos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Repos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(nameof(ChooseRepoInput.FullName))] ChooseRepoInput input)
        {
            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var checkRepo = await _context.Repos
                .FirstOrDefaultAsync(r => r.FullName.ToLower() == input.FullName.ToLower() && r.OwnerId == userId);

            if (checkRepo != null)
            {
                ModelState.AddModelError(nameof(ChooseRepoInput.FullName), "You already have this repo in your account");
                return View(input);
            }

            using var client = new HttpClient
            {
                BaseAddress = new Uri($"https://api.github.com/repos/{input.FullName}")
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("PrarallelTracker", "1.0"));

            Repo.RepoDeserialized repoDeserialized;

            try
            {
                repoDeserialized = await client.GetFromJsonAsync<Repo.RepoDeserialized>("");
            }
            catch (Exception ex)
            {
                if (ex is HttpRequestException reqEx &&
                    reqEx.StatusCode.GetValueOrDefault() == HttpStatusCode.NotFound)
                {
                    ModelState.AddModelError(nameof(ChooseRepoInput.FullName), "Couldn't find a repo with this name");
                    return View(input);
                }

                ModelState.AddModelError(string.Empty, "Something happend when trying to check the repo name");
                return View(input);
            }

            var repo = new Repo(repoDeserialized)
            {
                CopiedAt = DateTime.Now,
                OwnerId = userId
            };

            _context.Add(repo);
            await _context.SaveChangesAsync();

            TempData.AddAlertMessage(new AlertMessasge(AlertMessageType.Success, "Repo copied succesfully"));
            
            return RedirectToAction(nameof(Index));
        }

        private bool RepoExists(int id)
        {
            return _context.Repos.Any(e => e.Id == id);
            //async = new SelectListItem()
        }

        public class ChooseRepoInput
        {
            [Required]
            [DisplayName("Repo Full Name")]
            public string FullName { get; set; }
        }
    }

    public static class IssueSortModes
    {
        public const string Newest = nameof(Newest);
        public const string Oldest = nameof(Oldest);
        public const string MostComments = nameof(MostComments);
        public const string LeastComments = nameof(LeastComments);
    }
    public static class IssueStatus
    {
        public const string Open = nameof(Open);
        public const string Closed = nameof(Closed);
        public const string All = nameof(All);
    }

    public class IssueFilter
    {
        public bool YourIssues { get; set; }
        public string Text { get; set; }
        public string Status { get; set; }
        public string SortMode { get; set; }
    }

    public class RepoDetailsVm
    {
        public Repo Repo { get; set; }
        public IssueFilter Filter { get; set; }
        public SelectList SortModeSelectList { get; set; }
        public SelectList IssueStatusSelectList { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }



    public static class RepoSortModes
    {
        public const string Newest = nameof(Newest);
        public const string Oldest = nameof(Oldest);
    }

    public class RepoFilter
    {
        public string Text { get; set; }
        public string SortMode { get; set; }
    }

    public class RepoIndexVm
    {
        public IEnumerable<Repo> Repos { get; set; }
        public RepoFilter Filter { get; set; }
        public SelectList SortModeSelectList { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }
}
