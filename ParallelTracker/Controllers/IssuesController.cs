using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ParallelTracker.Data;
using ParallelTracker.Models;
using ParallelTracker.Tools;

namespace ParallelTracker.Controllers
{
    [Authorize]
    public class IssuesController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly CurrentResources _currentResources;

        public IssuesController(ApplicationContext context, CurrentResources currentResources)
        {
            _context = context;
            _currentResources = currentResources;
        }

        // GET: Issues/Details/5
        [AllowAnonymous]
        public IActionResult Details(int? id)
        {
            var issue = _currentResources.Issue;
            if (issue == null)
            {
                return NotFound();
            }

            return View(issue);
        }

        // GET: Issues/Create
        public async Task<IActionResult> Create(int? repoId)
        {
            var repo = await _context.Repos
                .FindAsync(repoId);
            if (repo == null)
            {
                TempData.AddAlertMessage(new AlertMessasge(AlertMessageType.Danger, "Please, select a repo first"));
                return RedirectToAction("Index", "Repos");
            }

            return View(new CreateIssueInput { RepoId = repo.Id });
        }

        // POST: Issues/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int? repoId, [Bind("RepoId,Title,Text")] CreateIssueInput input)
        {
            var repo = await _context.Repos
                .Include(r => r.Owner)
                .Include(r => r.Issues)
                    .ThenInclude(i => i.Author)
                .FirstOrDefaultAsync(r => r.Id == repoId);


            if (repo == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var issue = new Issue
                {
                    Title = input.Title,
                    Text = input.Text,
                    AuthorId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    RepoId = repo.Id,
                    CreatedAt = DateTime.Now,
                };

                _context.Add(issue);
                await _context.SaveChangesAsync();

                TempData.AddAlertMessage(new AlertMessasge(AlertMessageType.Success, "Issue opened succesfully"));

                return RedirectToAction(nameof(Details), new { id = issue.Id });
            }

            _currentResources.Repo = repo;
            return View(input);
        }

        // GET: Issues/Edit/5
        public IActionResult Edit(int? id)
        {
            var issue = _currentResources.Issue;
            if (issue == null)
            {
                return NotFound();
            }

            // Check Permission
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) != issue.AuthorId)
            {
                TempData.AddAlertMessage(new AlertMessasge(AlertMessageType.Danger, "Only authors can edit their issues"));
                return RedirectToAction(nameof(Details), new { id = issue.Id });
            }

            return View(new EditIssueInput
            {
                Title = issue.Title,
                Text = issue.Text
            });
        }

        // POST: Issues/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Title,Text")] EditIssueInput input)
        {
            var issue = _currentResources.Issue;
            if (issue == null)
            {
                return NotFound();
            }

            // Check Permission
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) != issue.AuthorId)
            {
                TempData.AddAlertMessage(new AlertMessasge(AlertMessageType.Danger, "Only authors can edit their issues"));
                return RedirectToAction(nameof(Details), new { id = issue.Id });
            }

            if (ModelState.IsValid)
            {
                if (input.Title == issue.Title && input.Text == issue.Text)
                {
                    TempData.AddAlertMessage(new AlertMessasge(AlertMessageType.Info, "You didn't modify the issue"));
                    return RedirectToAction(nameof(Details), new { id = issue.Id });
                }

                issue.Title = input.Title;
                issue.Text = input.Text;
                issue.EditedAt = DateTime.Now;

                try
                {
                    _context.Update(issue);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IssueExists(issue.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                TempData.AddAlertMessage(new AlertMessasge(AlertMessageType.Success, "Issue edited succesfully"));

                return RedirectToAction(nameof(Details), new { id = issue.Id });
            }
            return View(input);
        }

        // GET: Issues/Close/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int? id)
        {
            var issue = _currentResources.Issue;
            if (issue == null)
            {
                return NotFound();
            }

            // Check Permission
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) != issue.Repo.OwnerId)
            {
                TempData.AddAlertMessage(new AlertMessasge(AlertMessageType.Danger, "Only the repo owner can change its issues status"));
                return RedirectToAction(nameof(Details), new { id = issue.Id });
            }

            if (issue.IsClosed)
            {
                TempData.AddAlertMessage(new AlertMessasge(AlertMessageType.Danger, "Issue is already closed"));
                return RedirectToAction(nameof(Details), new { id = issue.Id });
            }

            issue.ClosedAt = DateTime.Now;

            try
            {
                _context.Update(issue);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!IssueExists(issue.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            TempData.AddAlertMessage(new AlertMessasge(AlertMessageType.Success, "Issue was closed successfully"));
            return RedirectToAction(nameof(Details), new { id = issue.Id });
        }

        // GET: Issues/Reopen/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(int? id)
        {
            var issue = _currentResources.Issue;
            if (issue == null)
            {
                return NotFound();
            }

            // Check Permission
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) != issue.Repo.OwnerId)
            {
                TempData.AddAlertMessage(new AlertMessasge(AlertMessageType.Danger, "Only the repo owner can change its issues status"));
                return RedirectToAction(nameof(Details), new { id = issue.Id });
            }

            if (!issue.IsClosed)
            {
                TempData.AddAlertMessage(new AlertMessasge(AlertMessageType.Danger, "Issue is already open"));
                return RedirectToAction(nameof(Details), new { id = issue.Id });
            }

            issue.ClosedAt = null;

            try
            {
                _context.Update(issue);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!IssueExists(issue.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            TempData.AddAlertMessage(new AlertMessasge(AlertMessageType.Success, "Issue was reopened successfully"));
            return RedirectToAction(nameof(Details), new { id = issue.Id });
        }

        private bool IssueExists(int id)
        {
            return _context.Issues.Any(e => e.Id == id);
        }
    }

    public class CreateIssueInput
    {
        [Required]
        public int RepoId { get; set; }
        [Required]
        [StringLength(128)]
        public string Title { get; set; }

        [Required]
        public string Text { get; set; }
    }

    public class EditIssueInput
    {
        [Required]
        [StringLength(128)]
        public string Title { get; set; }

        [Required]
        public string Text { get; set; }
    }
}
