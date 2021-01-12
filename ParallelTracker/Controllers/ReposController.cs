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
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var repo = await _context.Repos
                .Include(r => r.Owner)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (repo == null)
            {
                return NotFound();
            }

            return View(repo);
        }

        public IActionResult ChooseRepo()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChooseRepo([Bind(nameof(ChooseRepoInput.Name))] ChooseRepoInput input)
        {
            if (!ModelState.IsValid)
            {
                return View(input);
            }

            using var client = new HttpClient
            {
                BaseAddress = new Uri($"https://api.github.com/repos/{input.Name}")
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("PrarallelTracker", "1.0"));

            try
            {
                var repoDeserialized = await client.GetFromJsonAsync<Repo.RepoDeserialized>("");
            }
            catch (Exception ex)
            {
                if (ex is HttpRequestException reqEx &&
                    reqEx.StatusCode.GetValueOrDefault() == HttpStatusCode.NotFound)
                {
                    ModelState.AddModelError(nameof(ChooseRepoInput.Name), "Couldn't find a repo with this name");
                    return View(input);
                }

                ModelState.AddModelError(string.Empty, "Something happend when trying to check the repo name");
                return View(input);
            }

            return RedirectToAction(nameof(Create), new { repoName = input.Name });

        }

        // GET: Repos/Create
        public IActionResult Create(string repoName)
        {
            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Repos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Url,Name,FullName,AvatarUrl,Description,CreatedAt,ParalleledAt,GitHubOwnerLogin,GitHubOwnerUrl,OwnerId")] Repo repo)
        {
            if (ModelState.IsValid)
            {
                _context.Add(repo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "Id", repo.OwnerId);
            return View(repo);
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Url,Name,FullName,AvatarUrl,Description,CreatedAt,ParalleledAt,GitHubOwnerLogin,GitHubOwnerUrl,OwnerId")] Repo repo)
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
        }

        public class ChooseRepoInput
        {
            [Required]
            [DisplayName("Repo Name")]
            public string Name { get; set; }
        }
    }
}
