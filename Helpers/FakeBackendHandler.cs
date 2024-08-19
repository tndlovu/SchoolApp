using BlazorApp.Models;
using BlazorApp.Models.Account;
using BlazorApp.Models.Courses;
using BlazorApp.Pages.Courses;

//using BlazorApp.Pages.Courses;
using BlazorApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorApp.Helpers
{
    public class FakeBackendHandler : HttpClientHandler
    {
        private ILocalStorageService _localStorageService;

        public FakeBackendHandler(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // array in local storage for registered users
            var usersKey = "registered-users";
            var coursesKey = "courses";
            string _userKey = "user";

            var users = await _localStorageService.GetItem<List<UserRecord>>(usersKey) ?? new List<UserRecord>();
            //Console.WriteLine($"Before Update =>Total Users: {users.Count} ");
            var courses = await _localStorageService.GetItem<List<BlazorApp.Models.Course>>(coursesKey) ?? new List<BlazorApp.Models.Course>();

            var method = request.Method;
            var path = request.RequestUri.AbsolutePath;

            var studentCourseKey = "blazor-student-enroled-courses";
            //var studentCourses = await _localStorageService.GetItem<List<StudentCoursesRecord>>(studentCourseKey) ?? new List<StudentCoursesRecord>();
            
            return await handleRoute();

            async Task<HttpResponseMessage> handleRoute()
            {
                if (path == "/users/authenticate" && method == HttpMethod.Post)
                    return await authenticate();

                if (path == "/users/register" && method == HttpMethod.Post)
                    return await register();

                if (path == "/users" && method == HttpMethod.Get)
                    return await getUsers();

                if(path == "/courses/GetCoursesForUser" && method==HttpMethod.Get)
                    return await getCoursesForUser();

                if (path == "/courses/registercoursetostudent" && method == HttpMethod.Post)
                    return await registerCourseToStudent();

                if(path == "/courses/deregistercoursetostudent" && method == HttpMethod.Delete)
                    return await deRegisterCourseToStudent();

                //if (Regex.Match(path, @"\/courses/deregistercoursetostudent/courseID\/+$").Success && method == HttpMethod.Delete)
                //    return await deRegisterCourseToStudent();

                if (path == "/courses/register" && method == HttpMethod.Post)
                    return await registerCourses();

                if (Regex.Match(path, @"\/users\/\d+$").Success && method == HttpMethod.Get)
                    return await getUserById();

                if (Regex.Match(path, @"\/users\/\d+$").Success && method == HttpMethod.Put)
                    return await updateUser();

                if (Regex.Match(path, @"\/users\/\d+$").Success && method == HttpMethod.Delete)
                    return await deleteUser();

                if (path == "/courses/GetAll" && method == HttpMethod.Get)
                    return await getAllCourses();

                // pass through any requests not handled above
                return await base.SendAsync(request, cancellationToken);
            }

            // route functions
            
            async Task<HttpResponseMessage> authenticate()
            {
                var bodyJson = await request.Content.ReadAsStringAsync();
                var body = JsonSerializer.Deserialize<Login>(bodyJson);
                var user = users.FirstOrDefault(x => x.Username == body.Username && x.Password == body.Password);

                if (user == null)
                    return await error("Username or password is incorrect");

                return await ok(new {
                    Id = user.Id.ToString(),
                    Username = user.Username,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Token = "fake-jwt-token",
                    //EnroledCourse=new List<StudentCoursesRecord>
                    //    {
                    //    new StudentCoursesRecord
                    //        {
                    //         CourseId=1,
                    //          StudentId=user.Id
                    //        }
                    //    }
                });
            }

            async Task<HttpResponseMessage> register()
            {
                var bodyJson = await request.Content.ReadAsStringAsync();
                var body = JsonSerializer.Deserialize<AddUser>(bodyJson);

                if (users.Any(x => x.Username == body.Username))
                    return await error($"Username '{body.Username}' is already taken");

                var user = new UserRecord {
                    Id = users.Count > 0 ? users.Max(x => x.Id) + 1 : 1,
                    Username = body.Username,
                    Password = body.Password,
                    FirstName = body.FirstName,
                    LastName = body.LastName,                                            
                };

                users.Add(user);

                await _localStorageService.SetItem(usersKey, users);
                
                return await ok();
            }

            async Task<HttpResponseMessage> getCoursesForUser( )
                {
                if (!isLoggedIn()) return await unauthorized();
                //return await error("Getting Courses for user specified with ID:" + idFromPath());
                //return await ok(_localStorageService.GetItem<StudentCoursesRecord>(studentCourseKey));
                return await ok();
                }

            async Task<HttpResponseMessage> deRegisterCourseToStudent( )
                {
                if (!isLoggedIn()) return await unauthorized();
                var bodyJson = await request.Content.ReadAsStringAsync();
                var body = JsonSerializer.Deserialize<AddStudentCourse>(bodyJson);


                var user = await _localStorageService.GetItem<User>(_userKey);

                if (user == null)
                    {
                    //Console.WriteLine($"Could not find new user using key");
                    return await error("Could not find new user using key");
                    }

                var oldEnrolment = new StudentCourses
                    {
                    CourseId = body.CourseId,
                    StudentId = body.StudentId
                    };

                if (user.EnroledCourse.RemoveAll(enr => enr.CourseId == oldEnrolment.CourseId && enr.StudentId == oldEnrolment.StudentId)>0)
                    {
                    Console.WriteLine("Enrolment removed from the list");
                    await _localStorageService.RemoveItem(_userKey);
                    await _localStorageService.SetItem(_userKey, user);
                    }

                return await ok();
                }
            async Task<HttpResponseMessage> registerCourseToStudent()
                {
                if (!isLoggedIn()) return await unauthorized();
                var bodyJson = await request.Content.ReadAsStringAsync();
                var body = JsonSerializer.Deserialize<AddStudentCourse>(bodyJson);

                var user = users.FirstOrDefault(x => x.Id == int.Parse(body.StudentId));
                var newUser = await _localStorageService.GetItem<User>(_userKey);

                //return await error($"About to update user with ID {body.StudentId}.");
                if (user == null) 
                        return await error($"Could not update user with ID {body.StudentId}.");
                //Console.WriteLine($"Before Update User: ({user.FirstName}) has ({user.EnroledCourse.Count}) courses registered. ");

                if (newUser == null)
                    { 
                    Console.WriteLine($"Could not find new user using key"); 
                    }
                else
                    {
                    Console.WriteLine($"New use found and name is : {newUser.FirstName}");
                    }

                if (newUser.EnroledCourse == null)
                    {
                    newUser.EnroledCourse = new List<StudentCourses>();
                    }

                newUser.EnroledCourse.Add(new StudentCourses { 
                    CourseId= body.CourseId, 
                    StudentId= body.StudentId
                    });
                await _localStorageService.SetItem<User>(_userKey, newUser);
                return await ok();
                }

            async Task<HttpResponseMessage> registerCourses( )
                {
                if (!isLoggedIn()) return await unauthorized();
                var bodyJson = await request.Content.ReadAsStringAsync();
                var body = JsonSerializer.Deserialize<AddCourse>(bodyJson);
                BlazorApp.Models.Course courseRecord = new BlazorApp.Models.Course { 
                     Description = body.Description,
                      Name = body.Name,
                      Id=courses.Count>0? (courses.Count+1).ToString() : "1"
                    };
                courses.Add(courseRecord);

                return  await ok(_localStorageService.SetItem(studentCourseKey, courses));

                }
            async Task<HttpResponseMessage> getUsers()
            {
                if (!isLoggedIn()) return await unauthorized();
                return await ok(users.Select(x => basicDetails(x)));
            }

            async Task<HttpResponseMessage> getAllCourses( )
                {
                if (!isLoggedIn()) return await unauthorized();

                if (courses != null && courses.Count > 0)
                    return await ok(courses.ToList());

                courses = new List<BlazorApp.Models.Course> {
                    new BlazorApp.Models.Course
                        {
                        Id = "1",
                        Name = "English",
                        Description = "English First Language"
                        },
                    new BlazorApp.Models.Course
                        {
                        Id = "2",
                        Name = "Afrikaans",
                        Description = "Afrikaans"

                    },
                    new BlazorApp.Models.Course
                        {
                        Id = "3",
                        Name = "Mathematics",
                        Description = "Mathematics"
                        },
                    new BlazorApp.Models.Course
                        {
                        Id = "4",
                        Name = "Biology",
                        Description = "Bioloical Sciences"
                        }};

                return await ok(courses.ToList());
                }

            async Task<HttpResponseMessage> getUserById()
            {
                if (!isLoggedIn()) return await unauthorized();

                var user = users.FirstOrDefault(x => x.Id == idFromPath());
                return await ok(basicDetails(user));
            }

            async Task<HttpResponseMessage> updateUser() 
            {
                if (!isLoggedIn()) return await unauthorized();

                var bodyJson = await request.Content.ReadAsStringAsync();
                var body = JsonSerializer.Deserialize<EditUser>(bodyJson);
                var user = users.FirstOrDefault(x => x.Id == idFromPath());

                // if username changed check it isn't already taken
                if (user.Username != body.Username && users.Any(x => x.Username == body.Username))
                    return await error($"Username '{body.Username}' is already taken");

                // only update password if entered
                if (!string.IsNullOrWhiteSpace(body.Password))
                    user.Password = body.Password;

                // update and save user
                user.Username = body.Username;
                user.FirstName = body.FirstName;
                user.LastName = body.LastName;
                await _localStorageService.SetItem(usersKey, users);

                return await ok();
            }

            async Task<HttpResponseMessage> deleteUser()
            {
                if (!isLoggedIn()) return await unauthorized();

                users.RemoveAll(x => x.Id == idFromPath());
                await _localStorageService.SetItem(usersKey, users);

                return await ok();
            }

            // helper functions

            async Task<HttpResponseMessage> ok(object body = null)
            {
                return await jsonResponse(HttpStatusCode.OK, body ?? new {});
            }

            async Task<HttpResponseMessage> error(string message)
            {
                return await jsonResponse(HttpStatusCode.BadRequest, new { message });
            }

            async Task<HttpResponseMessage> unauthorized()
            {
                return await jsonResponse(HttpStatusCode.Unauthorized, new { message = "Unauthorized" });
            }

            async Task<HttpResponseMessage> jsonResponse(HttpStatusCode statusCode, object content)
            {
                var response = new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json")
                };
                
                // delay to simulate real api call
                await Task.Delay(500);

                return response;
            }

            bool isLoggedIn()
            {
                return request.Headers.Authorization?.Parameter == "fake-jwt-token";
            } 

            int idFromPath()
            {
                return int.Parse(path.Split('/').Last());
            }

            dynamic basicDetails(UserRecord user)
            {
                return new {
                    Id = user.Id.ToString(),
                    Username = user.Username,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                };
            }
        }
    }

    // class for user records stored by in-memory backend

    public class UserRecord {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public List<StudentCoursesRecord> EnroledCourse = new List<StudentCoursesRecord>();
        }

    //class for Course record stored by in-memory backend
    public class CourseRecord
        {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        }
    public class StudentCoursesRecord
        {
        public int CourseId { get; set; }
        public int StudentId { get; set; }
        }
}