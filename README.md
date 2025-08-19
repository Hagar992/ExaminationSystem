# 📘 ExaminationSystem

A simple **Examination System** in **C#** that manages courses, students, instructors, exams, and student attempts with both **auto-grading** and **manual grading** features.

## ✨ Features

* Manage **Courses** (add, update, delete).
* Manage **Students** and **Instructors**.
* Create **Exams** for courses with flexible question types:

  * Multiple Choice Questions (MCQ).
  * True/False Questions.
  * Essay/Descriptive Questions.
* Allow **Students** to attempt exams.
* **Auto-grading** for objective questions (MCQ & T/F).
* **Manual grading** for essay-type questions by instructors.
* View and store **results** and **grades**.

---

## 🏗️ System Components

### 1. **Entities (Models)**

* **Course**

  * Id, Name, Description.
* **Student**

  * Id, Name, Email, RegisteredCourses.
* **Instructor**

  * Id, Name, Specialization, CoursesTaught.
* **Exam**

  * Id, CourseId, List of Questions.
* **Question (Abstract Class)**

  * Id, Text, Marks.
  * Subclasses:

    * **MCQQuestion** (Choices, CorrectAnswer).
    * **TrueFalseQuestion** (CorrectAnswer).
    * **EssayQuestion** (Requires Manual Grading).
* **Attempt**

  * StudentId, ExamId, Answers, Grade.

---

### 2. **Core Logic**

* `ExamService` → Create exams, add questions.
* `StudentService` → Student registration, enroll in courses, attempt exams.
* `InstructorService` → Add courses, assign exams, grade essay questions.
* `GradingService` → Auto-grading & manual grading.

---

## ⚙️ Example Workflow

1. **Instructor** creates a course.
2. **Instructor** creates an exam with mixed question types.
3. **Student** enrolls in a course and takes the exam.
4. The system **auto-grades** objective questions.
5. The **Instructor** manually grades essay questions.
6. The system generates a **final grade report** for the student.

---

## 💻 Tech Stack

* **Language:** C#
* **Paradigm:** Object-Oriented Programming (OOP)
* **Framework:** .NET Console Application (or ASP.NET if extended)
* **Data Storage:** In-memory (can be extended to SQL/EF Core).

---

## 🚀 Future Improvements

* Web interface using **ASP.NET Core MVC**.
* Database support (SQL Server / EF Core).
* Authentication & Authorization for users.
* Export reports to **PDF/Excel**.

---

