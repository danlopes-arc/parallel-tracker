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

        // GET: Issues
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Issues.ToListAsync());
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
                .FindAsync(repoId);

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
                return RedirectToAction(nameof(Details), new { id = issue.Id });
            }
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

            if (User.FindFirstValue(ClaimTypes.NameIdentifier) != issue.AuthorId)
            {
                TempData.AddAlertMessage(new AlertMessasge(AlertMessageType.Danger, "Only authors can edit their issues"));
                return RedirectToAction(nameof(Details));
            }

            return View(new EditIssueInput
            {
                Title = issue.Title,
                Text = issue.Text,
                IsClosed = issue.IsClosed
            });
        }

        // POST: Issues/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Title,Text,IsClosed")] EditIssueInput input)
        {
            var issue = _currentResources.Issue;
            if (issue == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (input.Title != issue.Title || input.Text != issue.Text)
                {
                    issue.EditedAt = DateTime.Now;
                }
                issue.Title = input.Title;
                issue.Text = input.Text;
                if (input.IsClosed != issue.IsClosed)
                {
                    if (input.IsClosed)
                    {
                        issue.ClosedAt = DateTime.Now;
                    }
                    else
                    {
                        issue.ClosedAt = null;
                    }
                }

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
                return RedirectToAction(nameof(Details), new { id = issue.Id });
            }
            return View(input);
        }

        // GET: Issues/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var issue = await _context.Issues
                .FirstOrDefaultAsync(m => m.Id == id);
            if (issue == null)
            {
                return NotFound();
            }

            return View(issue);
        }

        // POST: Issues/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var issue = await _context.Issues.FindAsync(id);
            _context.Issues.Remove(issue);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
        [Required]
        [Display(Name ="Closed")]
        public bool IsClosed { get; set; }
    }
}
