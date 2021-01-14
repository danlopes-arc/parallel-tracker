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
    public class CommentsController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly CurrentResources _currentResources;

        public CommentsController(ApplicationContext context, CurrentResources currentResources)
        {
            _context = context;
            _currentResources = currentResources;
        }

        // GET: Comments
        public async Task<IActionResult> Index()
        {
            var applicationContext = _context.Comments.Include(c => c.Author).Include(c => c.Issue);
            return View(await applicationContext.ToListAsync());
        }

        // GET: Comments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments
                .Include(c => c.Author)
                .Include(c => c.Issue)
                    .ThenInclude(i => i.Repo)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        // GET: Comments/Create
        public IActionResult Create(int? issueId)
        {
            var issue = _currentResources.Issue;
            if (issue == null)
            {
                return NotFound();
            }
            return View(new CreateCommentInput { IssueId = issue.Id});
        }

        // POST: Comments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IssueId,Text")] CreateCommentInput input)
        {
            var issue = await _context.Issues
                .FirstOrDefaultAsync(i => i.Id == input.IssueId);

            if (issue == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var comment = new Comment
                {
                    AuthorId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    IssueId = issue.Id,
                    CreatedAt = DateTime.Now,
                    Text = input.Text
                };

                _context.Add(comment);
                await _context.SaveChangesAsync();

                TempData.AddAlertMessage(new AlertMessasge(AlertMessageType.Success, "Comment created succesfully"));

                return RedirectToAction(nameof(IssuesController.Details), Text.GetControllerName(typeof(IssuesController)), new { id = issue.Id });
            }
            return View(input);
        }

        // GET: Comments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }
            ViewData["AuthorId"] = new SelectList(_context.Users, "Id", "Id", comment.AuthorId);
            ViewData["IssueId"] = new SelectList(_context.Issues, "Id", "Id", comment.IssueId);
            return View(comment);
        }

        // POST: Comments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AuthorId,IssueId,Text,CreatedAt,EditedAt")] Comment comment)
        {
            if (id != comment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(comment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommentExists(comment.Id))
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
            ViewData["AuthorId"] = new SelectList(_context.Users, "Id", "Id", comment.AuthorId);
            ViewData["IssueId"] = new SelectList(_context.Issues, "Id", "Id", comment.IssueId);
            return View(comment);
        }

        // GET: Comments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments
                .Include(c => c.Author)
                .Include(c => c.Issue)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        // POST: Comments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CommentExists(int id)
        {
            return _context.Comments.Any(e => e.Id == id);
        }
    }

    public class CreateCommentInput
    {
        [Required]
        public int IssueId { get; set; }

        [Required]
        public string Text { get; set; }
    }
}
