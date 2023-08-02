﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LMS_CustomIdentity.Controllers
{
    [Authorize(Roles = "Professor")]
    public class ProfessorController : Controller
    {

        private readonly LMSContext db;

        public ProfessorController(LMSContext _db)
        {
            db = _db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Students(string subject, string num, string season, string year)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
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

        public IActionResult Categories(string subject, string num, string season, string year)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            return View();
        }

        public IActionResult CatAssignments(string subject, string num, string season, string year, string cat)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
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

        public IActionResult Submissions(string subject, string num, string season, string year, string cat, string aname)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
            ViewData["aname"] = aname;
            return View();
        }

        public IActionResult Grade(string subject, string num, string season, string year, string cat, string aname, string uid)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
            ViewData["aname"] = aname;
            ViewData["uid"] = uid;
            return View();
        }

        /*******Begin code to modify********/


        /// <summary>
        /// Returns a JSON array of all the students in a class.
        /// Each object in the array should have the following fields:
        /// "fname" - first name
        /// "lname" - last name
        /// "uid" - user ID
        /// "dob" - date of birth
        /// "grade" - the student's grade in this class
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetStudentsInClass(string subject, int num, string season, int year)
        {
            // Step 1: Get CatalogID using subject and num
            var catalogId = db.Courses
                              .Where(c => c.Department == subject && c.Number == num)
                              .Select(c => c.CatalogId)
                              .FirstOrDefault();

            if (catalogId == 0)
            {
                return Json(new { error = "Course not found" });
            }

            // Step 2: Find a Class using CatalogID
            var classId = db.Classes
                            .Where(c => c.Listing == catalogId && c.Season == season && c.Year == year)
                            .Select(c => c.ClassId)
                            .FirstOrDefault();

            if (classId == 0)
            {
                return Json(new { error = "Class not found" });
            }

            // Step 3: Join Class and Enrolled on ClassID, then join with Students
            var students = (from e in db.Enrolleds
                            join s in db.Students on e.Student equals s.UId
                            where e.Class == classId
                            select new
                            {
                                fname = s.FName,
                                lname = s.LName,
                                uid = s.UId,
                                dob = s.Dob,
                                grade = e.Grade
                            }).ToArray();

            return Json(students);
        }


        /// <summary>
        /// Returns a JSON array with all the assignments in an assignment category for a class.
        /// If the "category" parameter is null, return all assignments in the class.
        /// Each object in the array should have the following fields:
        /// "aname" - The assignment name
        /// "cname" - The assignment category name.
        /// "due" - The due DateTime
        /// "submissions" - The number of submissions to the assignment
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class, 
        /// or null to return assignments from all categories</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetAssignmentsInCategory(string subject, int num, string season, int year, string category)
        {
            var assignments = (from a in db.Assignments
                               join ac in db.AssignmentCategories on a.Category equals ac.CategoryId
                               join c in db.Classes on ac.InClass equals c.ClassId
                               join co in db.Courses on c.Listing equals co.CatalogId
                               join d in db.Departments on co.Department equals d.Subject
                               where d.Subject == subject && co.Number == num && c.Season == season && c.Year == year
                               && (category == null || ac.Name == category)
                               select new
                               {
                                   aname = a.Name,
                                   cname = ac.Name,
                                   due = a.Due,
                                   submissions = db.Submissions.Count(s => s.Assignment == a.AssignmentId)
                               }).ToList();

            return Json(assignments);
        }



        /// <summary>
        /// Returns a JSON array of the assignment categories for a certain class.
        /// Each object in the array should have the folling fields:
        /// "name" - The category name
        /// "weight" - The category weight
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetAssignmentCategories(string subject, int num, string season, int year)
        {
            var categories = (from ac in db.AssignmentCategories
                              join c in db.Classes on ac.InClass equals c.ClassId
                              join co in db.Courses on c.Listing equals co.CatalogId
                              join d in db.Departments on co.Department equals d.Subject
                              where d.Subject == subject && co.Number == num && c.Season == season && c.Year == year
                              select new
                              {
                                  name = ac.Name,
                                  weight = ac.Weight
                              }).ToList();

            return Json(categories);
        }


        /// <summary>
        /// Creates a new assignment category for the specified class.
        /// If a category of the given class with the given name already exists, return success = false.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The new category name</param>
        /// <param name="catweight">The new category weight</param>
        /// <returns>A JSON object containing {success = true/false} </returns>
        public IActionResult CreateAssignmentCategory(string subject, int num, string season, int year, string category, int catweight)
        {
            var classId = (from c in db.Classes
                           join co in db.Courses on c.Listing equals co.CatalogId
                           join d in db.Departments on co.Department equals d.Subject
                           where d.Subject == subject && co.Number == num && c.Season == season && c.Year == year
                           select c.ClassId).FirstOrDefault();

            if (classId == 0) // If no matching class is found, return success = false
            {
                return Json(new { success = false });
            }

            var existingCategory = db.AssignmentCategories
                .Where(ac => ac.InClass == classId && ac.Name == category)
                .FirstOrDefault();

            if (existingCategory != null) // If a category with the given name already exists, return success = false
            {
                return Json(new { success = false });
            }

            // Create the new category
            var newCategory = new AssignmentCategory
            {
                InClass = classId,
                Name = category,
                Weight = (uint)catweight
            };

            db.AssignmentCategories.Add(newCategory);
            db.SaveChanges();

            return Json(new { success = true });
        }



        /// <summary>
        /// Creates a new assignment for the given class and category.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The new assignment name</param>
        /// <param name="asgpoints">The max point value for the new assignment</param>
        /// <param name="asgdue">The due DateTime for the new assignment</param>
        /// <param name="asgcontents">The contents of the new assignment</param>
        /// <returns>A JSON object containing success = true/false</returns>
        public IActionResult CreateAssignment(string subject, int num, string season, int year, string category, string asgname, int asgpoints, DateTime asgdue, string asgcontents)
        {
            var classId = (from c in db.Classes
                           join co in db.Courses on c.Listing equals co.CatalogId
                           join d in db.Departments on co.Department equals d.Subject
                           where d.Subject == subject && co.Number == num && c.Season == season && c.Year == year
                           select c.ClassId).FirstOrDefault();

            if (classId == 0) // If no matching class is found, return success = false
            {
                return Json(new { success = false });
            }

            var categoryId = (from ac in db.AssignmentCategories
                              where ac.InClass == classId && ac.Name == category
                              select ac.CategoryId).FirstOrDefault();

            if (categoryId == 0) // If no matching category is found, return success = false
            {
                return Json(new { success = false });
            }

            var existingAssignment = db.Assignments
                .Where(a => a.Category == categoryId && a.Name == asgname)
                .FirstOrDefault();

            if (existingAssignment != null) // If an assignment with the given name already exists, return success = false
            {
                return Json(new { success = false });
            }

            // Create the new assignment
            var newAssignment = new Assignment
            {
                Category = categoryId,
                Name = asgname,
                MaxPoints = (uint)asgpoints,
                Due = asgdue,
                Contents = asgcontents
            };

            db.Assignments.Add(newAssignment);
            db.SaveChanges();

            return Json(new { success = true });
        }




        /// <summary>
        /// Gets a JSON array of all the submissions to a certain assignment.
        /// Each object in the array should have the following fields:
        /// "fname" - first name
        /// "lname" - last name
        /// "uid" - user ID
        /// "time" - DateTime of the submission
        /// "score" - The score given to the submission
        /// 
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The name of the assignment</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetSubmissionsToAssignment(string subject, int num, string season, int year, string category, string asgname)
        {
            var assignmentId = (from a in db.Assignments
                                join ac in db.AssignmentCategories on a.Category equals ac.CategoryId
                                join c in db.Classes on ac.InClass equals c.ClassId
                                join co in db.Courses on c.Listing equals co.CatalogId
                                join d in db.Departments on co.Department equals d.Subject
                                where d.Subject == subject && co.Number == num && c.Season == season && c.Year == year && ac.Name == category && a.Name == asgname
                                select a.AssignmentId).FirstOrDefault();

            if (assignmentId == 0) // If no matching assignment is found
            {
                return Json(new { success = false });
            }

            var submissions = (from s in db.Submissions
                               join a in db.Assignments on s.Assignment equals a.AssignmentId
                               join u in db.Students on s.Student equals u.UId
                               where a.AssignmentId == assignmentId
                               select new
                               {
                                   fname = u.FName,
                                   lname = u.LName,
                                   uid = u.UId,
                                   time = s.Time,
                                   score = s.Score
                               }).ToList();

            return Json(submissions);
        }



        /// <summary>
        /// Set the score of an assignment submission
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The name of the assignment</param>
        /// <param name="uid">The uid of the student who's submission is being graded</param>
        /// <param name="score">The new score for the submission</param>
        /// <returns>A JSON object containing success = true/false</returns>
        public IActionResult GradeSubmission(string subject, int num, string season, int year, string category, string asgname, string uid, int score)
        {
            var assignmentId = (from a in db.Assignments
                                join ac in db.AssignmentCategories on a.Category equals ac.CategoryId
                                join c in db.Classes on ac.InClass equals c.ClassId
                                join co in db.Courses on c.Listing equals co.CatalogId
                                join d in db.Departments on co.Department equals d.Subject
                                where d.Subject == subject && co.Number == num && c.Season == season && c.Year == year && ac.Name == category && a.Name == asgname
                                select a.AssignmentId).FirstOrDefault();

            if (assignmentId == 0) // If no matching assignment is found, return success = false
            {
                return Json(new { success = false });
            }

            var submission = db.Submissions
                .Where(s => s.Assignment == assignmentId && s.Student == uid)
                .FirstOrDefault();

            if (submission == null) // If no matching submission is found, return success = false
            {
                return Json(new { success = false });
            }

            submission.Score = (uint)score; // Update the score
            db.SaveChanges();

            return Json(new { success = true });
        }



        /// <summary>
        /// Returns a JSON array of the classes taught by the specified professor
        /// Each object in the array should have the following fields:
        /// "subject" - The subject abbreviation of the class (such as "CS")
        /// "number" - The course number (such as 5530)
        /// "name" - The course name
        /// "season" - The season part of the semester in which the class is taught
        /// "year" - The year part of the semester in which the class is taught
        /// </summary>
        /// <param name="uid">The professor's uid</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetMyClasses(string uid)
        {
            var classes = (from c in db.Classes
                           join co in db.Courses on c.Listing equals co.CatalogId
                           join p in db.Professors on c.TaughtBy equals p.UId
                           where p.UId == uid
                           select new
                           {
                               subject = co.Department,
                               number = co.Number,
                               name = co.Name,
                               season = c.Season,
                               year = c.Year
                           }).ToList();

            return Json(classes);
        }




        /*******End code to modify********/
    }
}

