using BlazorApp.Helpers;
using BlazorApp.Models;
using BlazorApp.Models.Account;
using BlazorApp.Models.Courses;

using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


namespace BlazorApp.Services
{

    public interface ICoursesService
        {
        User User { get; set; }
        Task<IList<Course>>  GetAllCourses();
        Task<IList<Course>> GetCourseForUser(string userId);
        IList<Models.Course> Courses { get; }
        Task Initialize();
        Task RegisterStudentToCourse(string courseId, string studentId);
        Task DeregisterStudentToCourse(string courseId, string studentId);
        Task Register(AddCourse model);
        Task DeleteCourse(string courseId);
        }
    public class CoursesService : ICoursesService
        {
        private IHttpService _httpService;
        private NavigationManager _navigationManager;
        private ILocalStorageService _localStorageService;
        private string _courseKey = "courses";
        private string _studentCourseKey = "blazor-student-enroled-courses";
        public User User { get;  set; }
        public Course Course { get; private set; }
        public IList<Models.Course> Courses { get; private set; }
        

        public CoursesService(IHttpService httpService,
            NavigationManager navigationManager,
            ILocalStorageService localStorageService)
            {
            _httpService = httpService;
            _navigationManager = navigationManager;
            _localStorageService = localStorageService;
            Courses = new List<Models.Course>();
            
            }
        public async Task Initialize( )
            {
           
            Courses = await GetAllCourses();
            await _localStorageService.SetItem(_courseKey, Courses);
            }

        public async Task<IList<Course>>  GetAllCourses()
            {
            return await _httpService.Get<IList<Course>>("/courses/GetAll");
            }

        public async Task<IList<Course>> GetCourseForUser(string userId)
            {
            return await _httpService.Get<IList<Course>>("/courses/GetCoursesForUser" + userId);
            }

        public async Task DeleteCourse(string courseId)
            {
            Course cousrseToDelete = Courses.FirstOrDefault(c => c.Id == courseId);
            if (cousrseToDelete != null)
                {
                Console.WriteLine("You wish to delete the couse with ID:" + courseId);
                }
             Courses.Remove(cousrseToDelete);
            await _localStorageService.RemoveItem(_courseKey);

            await _localStorageService.SetItem(_courseKey, Courses);
            }
        public async Task Register(AddCourse model)
            {
            //await _httpService.Post("/courses/register", model);
            Courses = await _localStorageService.GetItem<List<Course>>(_courseKey);
            if(Courses == null || Courses.Count<=0) 
                { 
                Courses = await GetAllCourses();
                }
            Console.WriteLine("Courses found from memory are : " + Courses.Count);

            Course courseRecord = new Course
                {
                Description = model.Description,
                Name = model.Name,
                Id = Courses.Count > 0 ? (Courses.Count + 1).ToString() : "1"
                };
            Courses.Add(courseRecord);
            await _localStorageService.RemoveItem(_courseKey);

            await _localStorageService.SetItem(_courseKey, Courses);
            }

        public async Task RegisterStudentToCourse(string courseId, string studentId)
            {
            AddStudentCourse addStudentCourse = new AddStudentCourse {
                 CourseId = courseId,
                 StudentId = studentId
                };
            await _httpService.Post("/courses/registercoursetostudent", addStudentCourse); 
            //await _localStorageService.SetItem(_studentCourseKey, addStudentCourse);
            }
        public async Task DeregisterStudentToCourse(string courseId, string studentId)
            {
            AddStudentCourse addStudentCourse = new AddStudentCourse
                {
                CourseId = courseId,
                StudentId = studentId
                };
            await _httpService.Delete< AddStudentCourse>("/courses/deregistercoursetostudent", addStudentCourse);
            //await _httpService.Delete($"/courses/deregistercoursetostudent/courseID:{courseId}/studentId:{studentId}");
            }
        }
        
    }
