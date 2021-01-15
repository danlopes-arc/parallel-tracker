using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelTracker.Models
{
    public class Issue
    {
        public int Id { get; set; }
        public string AuthorId { get; set; }
        public int RepoId { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public User Author { get; set; }
        public Repo Repo { get; set; }

        public IEnumerable<Comment> Comments { get; set; }

        public bool IsClosed => ClosedAt != null;
        public bool IsEdited => EditedAt != null;

        public IEnumerable<Comment> FindCommentsByText(string text)
        {
            return Comments?.Where(c => c.Text.ToLower().Contains(text.ToLower()));
        }
    }
}
