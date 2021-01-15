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
        private readonly ApplicationContext _context;

        public ReposController(ApplicationContext context)
        {
            _context = context;
        }

        // GET: Repos
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var repos = await _context.Repos
                .Include(r => r.Owner)
                .ToListAsync();

            return View(repos);
        }

        // GET: Repos/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id, [Bind] IssueFilter issueFilter, int? page)
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

            issueFilter.SortMode ??= SortModes.Newest;
            switch (issueFilter.SortMode)
            {
                case SortModes.Oldest:
                    repo.Issues = repo.Issues.OrderBy(i => i.CreatedAt);
                    break;
                case SortModes.MostComments:
                    repo.Issues = repo.Issues.OrderByDescending(i => i.Comments.Count());
                    break;
                case SortModes.LeastComments:
                    repo.Issues = repo.Issues.OrderBy(i => i.Comments.Count());
                    break;
                case SortModes.Newest:
                default:
                    repo.Issues = repo.Issues.OrderByDescending(i => i.CreatedAt);
                    issueFilter.SortMode = SortModes.Newest;
                    break;
            }
            var sortModeSelectList = new SelectList(new[]
            {
                new { Code = SortModes.Newest, Text = "Newest"},
                new { Code = SortModes.Oldest, Text = "Oldest"},
                new { Code = SortModes.MostComments, Text = "Most Comments"},
                new { Code = SortModes.LeastComments, Text = "Least Comments"},
            }, "Code", "Text", issueFilter.SortMode);


            issueFilter.Status ??= IssueStatus.All;
            if (issueFilter.YourIssues && User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                repo.Issues = repo.Issues.Where(i => i.AuthorId == userId);
            }
            switch (issueFilter.Status)
            {
                case IssueStatus.Closed:
                    repo.Issues = repo.Issues.Where(i => i.IsClosed);
                    break;
                case IssueStatus.Open:
                    repo.Issues = repo.Issues.Where(i => !i.IsClosed);
                    break;
                case IssueStatus.All:
                default:
                    issueFilter.Status = IssueStatus.All;
                    break;
            }
            var issueStatusSelectList = new SelectList(new[]
            {
                new { Code = IssueStatus.All, Text = "All Issues"},
                new { Code = IssueStatus.Open, Text = "Open Issues"},
                new { Code = IssueStatus.Closed, Text = "Closed Issues"},
            }, "Code", "Text", issueFilter.Status);

            if (issueFilter.YourIssues && User.Identity.IsAuthenticated)
            {
                repo.Issues = repo.Issues.Where(r => r.AuthorId == User.FindFirstValue(ClaimTypes.NameIdentifier));
            }

            if (!string.IsNullOrEmpty(issueFilter.Text))
            {
                var lowerText = issueFilter.Text.ToLower();
                await _context.Comments
                    .Where(c => c.Issue.RepoId == repo.Id)
                    .ToListAsync();

                repo.Issues = repo.Issues
                    .Where(i => i.Title.ToLower().Contains(lowerText) ||
                    i.Text.ToLower().Contains(lowerText) ||
                    (i.FindCommentsByText(lowerText)?.Count() ?? 0) > 0);
            }

            page  = Math.Max(page.GetValueOrDefault(), 1);
            var totalPages = (int)Math.Ceiling(repo.Issues.Count() / 2f);

            if (page > 0)
            {
                repo.Issues = repo.Issues
                    .Skip(2 * (page.Value - 1))
                    .Take(2);
            }

            return View(new RepoDetailsVm
            {
                Repo = repo,
                IssueFilter = issueFilter,
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
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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

        // GET: Repos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var repo = await _context.Repos.FindAsync(id);
            if (repo == null)
            {
                return NotFound();
            }
            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "Id", repo.OwnerId);
            return View(repo);
        }

        // POST: Repos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Url,Name,FullName,AvatarUrl,Description,CreatedAt,CopiedAt,GitHubOwnerLogin,GitHubOwnerUrl,OwnerId")] Repo repo)
        {
            if (id != repo.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(repo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RepoExists(repo.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "Id", repo.OwnerId);
            return View(repo);
        }

        // GET: Repos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var repo = await _context.Repos
                .Include(r => r.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (repo == null)
            {
                return NotFound();
            }

            return View(repo);
        }

        // POST: Repos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var repo = await _context.Repos.FindAsync(id);
            _context.Repos.Remove(repo);
            await _context.SaveChangesAsync();
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

    public static class SortModes
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
        public IssueFilter IssueFilter { get; set; }
        public SelectList SortModeSelectList { get; set; }
        public SelectList IssueStatusSelectList { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }
}
