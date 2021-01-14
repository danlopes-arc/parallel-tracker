using System;
using System.Collections.Generic;

namespace ParallelTracker.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string AuthorId { get; set; }
        public int IssueId { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }

        public User Author { get; set; }
        public Issue Issue { get; set; }

        public bool IsEdited => EditedAt != null;
    }
}