using System.ComponentModel.DataAnnotations;
namespace BlazorApp.Models.Courses
    {
    public class AddCourse
        {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        }
    }
