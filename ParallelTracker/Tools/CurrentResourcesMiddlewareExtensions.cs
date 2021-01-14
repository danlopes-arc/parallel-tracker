using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ParallelTracker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelTracker.Tools
{
    public class CurrentResourcesMiddleware
    {
        private readonly RequestDelegate _next;

        public CurrentResourcesMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext httpContext, CurrentResources resources, ApplicationContext dbContext)
        {

            var controller = httpContext.Request.RouteValues["controller"]?.ToString();
            var id = httpContext.Request.RouteValues["id"]?.ToString();
            string repoId = httpContext.Request.Query["repoId"];
            string issueId = httpContext.Request.Query["issueId"];

            if (controller == "Repos") repoId = id;
            if (controller == "Issues") issueId = id;

            if (int.TryParse(issueId, out int issueIntId))
            {
                resources.Issue = await dbContext.Issues
                    .Include(i => i.Author)
                    .Include(i => i.Repo)
                        .ThenInclude(r => r.Owner)
                    .Include(i => i.Repo)
                        .ThenInclude(r => r.Issues)
                    .Include(i => i.Comments)
                    .FirstOrDefaultAsync(i => i.Id == issueIntId);

                if (resources.Issue != null)
                {
                    resources.Repo = resources.Issue.Repo;
                }
            }

            if (resources.Issue == null && int.TryParse(repoId, out int intRepoId))
            {
                resources.Repo = await dbContext.Repos
                    .Include(r => r.Owner)
                    .Include(r => r.Issues)
                    .FirstOrDefaultAsync(r => r.Id == intRepoId);
            }

            await _next(httpContext);
        }
    }
    public static class CurrentResourcesMiddlewareExtensions
    {
        public static IApplicationBuilder UseCurrentResources(
               this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CurrentResourcesMiddleware>();
        }
    }
}
