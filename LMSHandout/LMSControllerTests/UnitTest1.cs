using LMS.Controllers;
using LMS.Models.LMSModels;
using LMS_CustomIdentity.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LMSControllerTests
{
    public class UnitTest1
    {
        // Uncomment the methods below after scaffolding
        // (they won't compile until then)
        [Fact]
        public void Test1()
        {
            // An example of a simple unit test on the CommonController
            CommonController ctrl = new CommonController(MakeTinyDB());

            var allDepts = ctrl.GetDepartments() as JsonResult;
            Assert.NotNull(allDepts);
            Assert.NotNull(allDepts.Value);

            // Convert to a collection of anonymous types
            var departments = (allDepts.Value as IEnumerable<object>)?.ToList();
            Assert.NotNull(departments);
            Assert.Single(departments);

            // Use reflection to access properties
            var firstDepartment = departments[0];
            var subject = firstDepartment.GetType().GetProperty("subject")?.GetValue(firstDepartment);
            Assert.Equal("CS", subject);
        }



        ///// <summary>
        ///// Make a very tiny in-memory database, containing just one department
        ///// and nothing else.
        ///// </summary>
        ///// <returns></returns>
        LMSContext MakeTinyDB()
        {
            var contextOptions = new DbContextOptionsBuilder<LMSContext>()
                .UseInMemoryDatabase("LMSControllerTest")
                .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .UseApplicationServiceProvider(NewServiceProvider())
                .Options;

            var db = new LMSContext(contextOptions);

            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Adding a department
            db.Departments.Add(new Department { Name = "KSoC", Subject = "CS" });

            // Adding a course
            db.Courses.Add(new Course { CatalogId = 1, Name = "Introduction to Computer Science", Number = 5530, Department = "CS" });

            // Adding a professor
            db.Professors.Add(new Professor { UId = "u1234567", FName = "John", LName = "Doe", WorksIn = "CS"});

            // Adding a class
            db.Classes.Add(new Class { ClassId = 1, Season = "Fall", Year = 2023, Listing = 1, TaughtBy = "u1234567", Location = "TEST" });

            // Adding a student
            db.Students.Add(new Student { UId = "u7654321", FName = "Jane", LName = "Smith", Major = "CS" });

            // Adding an assignment category
            db.AssignmentCategories.Add(new AssignmentCategory { CategoryId = 1, InClass = 1, Name = "Homework", Weight = 50 });

            // Adding an assignment
            db.Assignments.Add(new Assignment { AssignmentId = 1, Category = 1, Name = "HW1", MaxPoints = 100, Due = DateTime.Now, Contents = "TESTING" });

            db.SaveChanges();

            return db;
        }

        private IServiceProvider? NewServiceProvider()
        {
            var serviceProvider = new ServiceCollection()
                .AddDbContext<LMSContext>()
                .BuildServiceProvider();

            return serviceProvider;
        }
    }
}