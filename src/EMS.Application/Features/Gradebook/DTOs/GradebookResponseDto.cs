using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Gradebook.DTOs
{
    public class GradebookResponseDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public List<GradebookColumnDto> Columns { get; set; } = new List<GradebookColumnDto>();
        public List<GradebookStudentRowDto> StudentRows { get; set; } = new List<GradebookStudentRowDto>();
    }

    public class GradebookColumnDto
    {
        public Guid AssignmentId { get; set; }
        public string Title { get; set; } = null!;
        public Guid? GradeCategoryId { get; set; }
        public string GradeCategoryName { get; set; } = null!;
        public decimal Weight { get; set; }
    }

    public class GradebookStudentRowDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public List<StudentGradeEntryDto> Grades { get; set; } = new List<StudentGradeEntryDto>();
        public decimal FinalAverage { get; set; }
    }

    public class StudentGradeEntryDto
    {
        public Guid AssignmentId { get; set; }
        public Guid? SubmissionId { get; set; }
        public decimal? Grade { get; set; }
    }
}
