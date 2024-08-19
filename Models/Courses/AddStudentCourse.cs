using System.ComponentModel.DataAnnotations;
namespace BlazorApp.Models.Courses
    {
    public class AddStudentCourse
        {
        [Required]
        public string CourseId { get; set; }
        [Required]
        public string StudentId { get;set; }
        }
    }
