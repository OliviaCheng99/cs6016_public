using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LMS.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private LMSContext db;
        public StudentController(LMSContext _db)
        {
            db = _db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Catalog()
        {
            return View();
        }

        public IActionResult Class(string subject, string num, string season, string year)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            return View();
        }

        public IActionResult Assignment(string subject, string num, string season, string year, string cat, string aname)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
            ViewData["aname"] = aname;
            return View();
        }


        public IActionResult ClassListings(string subject, string num)
        {
            System.Diagnostics.Debug.WriteLine(subject + num);
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            return View();
        }


        /*******Begin code to modify********/

        /// <summary>
        /// Returns a JSON array of the classes the given student is enrolled in.
        /// Each object in the array should have the following fields:
        /// "subject" - The subject abbreviation of the class (such as "CS")
        /// "number" - The course number (such as 5530)
        /// "name" - The course name
        /// "season" - The season part of the semester
        /// "year" - The year part of the semester
        /// "grade" - The grade earned in the class, or "--" if one hasn't been assigned
        /// </summary>
        /// <param name="uid">The uid of the student</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetMyClasses(string uid)
        {
            var classes = (from e in db.Enrolleds
                           join c in db.Classes on e.Class equals c.ClassId
                           join co in db.Courses on c.Listing equals co.CatalogId
                           where e.Student == uid
                           select new
                           {
                               subject = co.Department,
                               number = co.Number,
                               name = co.Name,
                               season = c.Season,
                               year = c.Year,
                               grade = e.Grade ?? "--"
                           }).ToArray();

            return Json(classes);
        }

        /// <summary>
        /// Returns a JSON array of all the assignments in the given class that the given student is enrolled in.
        /// Each object in the array should have the following fields:
        /// "aname" - The assignment name
        /// "cname" - The category name that the assignment belongs to
        /// "due" - The due Date/Time
        /// "score" - The score earned by the student, or null if the student has not submitted to this assignment.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="uid"></param>
        /// <returns>The JSON array</returns>
        public IActionResult GetAssignmentsInClass(string subject, int num, string season, int year, string uid)
        {
            // Find the specific class using the provided parameters
            var classInfo = db.Classes
                              .Include(c => c.ListingNavigation) // Include Courses
                              .FirstOrDefault(c => c.ListingNavigation.Department == subject
                                                   && c.ListingNavigation.Number == num
                                                   && c.Season == season
                                                   && c.Year == year);
            if (classInfo == null)
            {
                return Json(new { error = "Class not found" });
            }

            // Check if the student is enrolled in the class
            var enrollment = db.Enrolleds.FirstOrDefault(e => e.Class == classInfo.ClassId && e.Student == uid);
            if (enrollment == null)
            {
                return Json(new { error = "Student not enrolled in the class" });
            }

            // Retrieve the assignments for the class
            var assignments = db.Assignments
                                .Include(a => a.CategoryNavigation) // Include AssignmentCategories
                                .Where(a => a.CategoryNavigation.InClass == classInfo.ClassId)
                                .Select(a => new
                                {
                                    aname = a.Name,
                                    cname = a.CategoryNavigation.Name,
                                    due = a.Due,
                                    score = db.Submissions
                                              .Where(s => s.Assignment == a.AssignmentId && s.Student == uid)
                                              .Select(s => (int?)s.Score) // Cast to nullable int
                                              .FirstOrDefault()
                                })
                                .ToList();

            return Json(assignments);
        }





        /// <summary>
        /// Adds a submission to the given assignment for the given student
        /// The submission should use the current time as its DateTime
        /// You can get the current time with DateTime.Now
        /// The score of the submission should start as 0 until a Professor grades it
        /// If a Student submits to an assignment again, it should replace the submission contents
        /// and the submission time (the score should remain the same).
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The new assignment name</param>
        /// <param name="uid">The student submitting the assignment</param>
        /// <param name="contents">The text contents of the student's submission</param>
        /// <returns>A JSON object containing {success = true/false}</returns>
        public IActionResult SubmitAssignmentText(string subject, int num, string season, int year,
    string category, string asgname, string uid, string contents)
        {
            try
            {
                // Find the course using the subject and course number
                var course = db.Courses.FirstOrDefault(c => c.Department == subject && c.Number == num);

                if (course == null)
                {
                    return Json(new { success = false });
                }

                // Find the class using the season and year, and the course's CatalogID
                var classInfo = db.Classes.FirstOrDefault(c => c.Season == season && c.Year == year && c.Listing == course.CatalogId);

                if (classInfo == null)
                {
                    return Json(new { success = false });
                }

                // Find the assignment category using the class's ClassID and the provided category name
                var assignmentCategory = db.AssignmentCategories.FirstOrDefault(ac => ac.InClass == classInfo.ClassId && ac.Name == category);

                if (assignmentCategory == null)
                {
                    return Json(new { success = false });
                }

                // Finally, find the assignment using the category's CategoryID and the provided assignment name
                var assignment = db.Assignments.FirstOrDefault(a => a.Category == assignmentCategory.CategoryId && a.Name == asgname);


                // Check if assignment exists
                if (assignment == null)
                {
                    return Json(new { success = false, message = "Assignment not found" });
                }

                // Find existing submission or create new one
                var submission = db.Submissions
                    .Where(s => s.Assignment == assignment.AssignmentId && s.Student == uid)
                    .FirstOrDefault();

                if (submission == null)
                {
                    submission = new Submission
                    {
                        Assignment = assignment.AssignmentId,
                        Student = uid,
                        Score = 0, // Initially 0, until graded by Professor
                        SubmissionContents = contents,
                        Time = DateTime.Now // Current time
                    };
                    db.Submissions.Add(submission);
                }
                else
                {
                    submission.SubmissionContents = contents;
                    submission.Time = DateTime.Now; // Update time
                }

                db.SaveChanges(); 

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Handle any exceptions and return a failure response
                return Json(new { success = false, message = ex.Message });
            }
        }



        /// <summary>
        /// Enrolls a student in a class.
        /// </summary>
        /// <param name="subject">The department subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester</param>
        /// <param name="year">The year part of the semester</param>
        /// <param name="uid">The uid of the student</param>
        /// <returns>A JSON object containing {success = {true/false}. 
        /// false if the student is already enrolled in the class, true otherwise.</returns>
        public IActionResult Enroll(string subject, int num, string season, int year, string uid)
        {
            // Find the course using the subject and course number
            var course = db.Courses.FirstOrDefault(c => c.Department == subject && c.Number == num);

            if (course == null)
            {
                return Json(new { success = false });
            }

            // Find the class using the season and year, and the course's CatalogID
            var classInfo = db.Classes.FirstOrDefault(c => c.Season == season && c.Year == year && c.Listing == course.CatalogId);

            if (classInfo == null)
            {
                return Json(new { success = false });
            }

            // Find the student using the uid
            var student = db.Students.FirstOrDefault(s => s.UId == uid);

            if (student == null)
            {
                return Json(new { success = false });
            }

            // Check if the student is already enrolled in the class
            var existingEnrollment = db.Enrolleds.FirstOrDefault(e => e.Class == classInfo.ClassId && e.Student == student.UId);

            if (existingEnrollment != null)
            {
                // Student is already enrolled
                return Json(new { success = false });
            }

            // Create a new enrollment
            var enrollment = new Enrolled
            {
                Student = uid,
                Class = classInfo.ClassId,
                Grade = "N/A" // or some default value for Grade
            };

            // Add the new enrollment to the database context
            db.Enrolleds.Add(enrollment);
            db.SaveChanges();

            return Json(new { success = true });
        }




        /// <summary>
        /// Calculates a student's GPA
        /// A student's GPA is determined by the grade-point representation of the average grade in all their classes.
        /// Assume all classes are 4 credit hours.
        /// If a student does not have a grade in a class ("--"), that class is not counted in the average.
        /// If a student is not enrolled in any classes, they have a GPA of 0.0.
        /// Otherwise, the point-value of a letter grade is determined by the table on this page:
        /// https://advising.utah.edu/academic-standards/gpa-calculator-new.php
        /// </summary>
        /// <param name="uid">The uid of the student</param>
        /// <returns>A JSON object containing a single field called "gpa" with the number value</returns>
        public IActionResult GetGPA(string uid)
        {
            double totalPoints = 0.0;
            int totalClasses = 0;

            // Query the enrolled classes and grades for the specified student
            var enrollments = db.Enrolleds.Where(e => e.Student == uid);

            // Iterate through the enrollments to calculate total points
            foreach (var enrollment in enrollments)
            {
                var grade = enrollment.Grade;

                // Determine the point value for the letter grade
                double pointValue = grade switch
                {
                    "A" => 4.0,
                    "A-" => 3.7,
                    "B+" => 3.3,
                    "B" => 3.0,
                    "B-" => 2.7,
                    "C+" => 2.3,
                    "C" => 2.0,
                    "C-" => 1.7,
                    "D+" => 1.3,
                    "D" => 1.0,
                    "D-" => 0.7,
                    "E" => 0.0,
                    "--" => -1.0, // Special case to skip classes without a grade
                    _ => throw new InvalidOperationException("Unknown grade")
                };

                if (pointValue >= 0)
                {
                    totalPoints += pointValue;
                    totalClasses++;
                }
            }

            // Calculate the GPA
            double gpa = totalClasses > 0 ? totalPoints / totalClasses : 0.0;

            return Json(new { gpa = gpa });
        }


        /*******End code to modify********/

    }
}

