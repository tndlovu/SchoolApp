using System.Collections.Generic;
using BlazorApp.Models.Courses;

namespace BlazorApp.Models
    {
    public class UserAccount
        {
        public User User { get;set; }
        public List<StudentCourses> EnroledCourse { get; set; }
        }
    }
