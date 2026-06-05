using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.ProgressReports.DTOs
{
    public class ClassReportSummaryItemDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public string? Room { get; set; }

        public int TotalStudents { get; set; }
        public int ReadyCount { get; set; }      
        public int PublishedCount { get; set; }  
        public int DraftCount => TotalStudents - ReadyCount - PublishedCount; 
        public int CreatedReports => ReadyCount + PublishedCount; 
        public double CompletionRate { get; set; } 
        public DateTime Deadline { get; set; }
        public bool IsNearDeadline { get; set; } 

        public DateTime? LastUpdated { get; set; } 
    }
}
