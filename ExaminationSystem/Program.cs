using System;
using System.Collections.Generic;
using System.Linq;

namespace ExaminationSystem
{
    // ===================== Domain: Core =====================
    public class Course
    {
        private static int _seq = 0;
        public int Id { get; } = ++_seq;
        public string Title { get; }
        public string Description { get; }
        public int MaxDegree { get; }

        public Course(string title, string description, int maxDegree)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title required.");
            if (maxDegree <= 0) throw new ArgumentException("MaxDegree must be > 0.");
            Title = title.Trim();
            Description = description?.Trim() ?? "";
            MaxDegree = maxDegree;
        }

        public override string ToString() => $"{Title} (Max: {MaxDegree})";
    }

    public class Student
    {
        private static int _seq = 0;
        public int Id { get; } = ++_seq;
        public string Name { get; private set; }
        public string Email { get; private set; }
        public HashSet<int> EnrolledCourseIds { get; } = new();

        public Student(string name, string email)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.");
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email required.");
            Name = name.Trim();
            Email = email.Trim();
        }

        public void Enroll(Course c) => EnrolledCourseIds.Add(c.Id);
        public void Update(string? name = null, string? email = null)
        {
            if (!string.IsNullOrWhiteSpace(name)) Name = name.Trim();
            if (!string.IsNullOrWhiteSpace(email)) Email = email.Trim();
        }

        public override string ToString() => $"{Name} (#{Id})";
    }

    public class Instructor
    {
        private static int _seq = 0;
        public int Id { get; } = ++_seq;
        public string Name { get; private set; }
        public string Specialization { get; private set; }
        public HashSet<int> TeachesCourseIds { get; } = new();

        public Instructor(string name, string specialization)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.");
            Name = name.Trim();
            Specialization = specialization?.Trim() ?? "";
        }

        public void AssignTo(Course c) => TeachesCourseIds.Add(c.Id);
        public void Update(string? name = null, string? specialization = null)
        {
            if (!string.IsNullOrWhiteSpace(name)) Name = name.Trim();
            if (!string.IsNullOrWhiteSpace(specialization)) Specialization = specialization.Trim();
        }

        public override string ToString() => $"{Name} - {Specialization}";
    }

    // ===================== Domain: Questions =====================
    public abstract class Question
    {
        private static int _seq = 0;
        public int Id { get; } = ++_seq;
        public string Text { get; private set; }
        public int Mark { get; private set; }

        protected Question(string text, int mark)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Question text required.");
            if (mark <= 0) throw new ArgumentException("Mark must be > 0.");
            Text = text.Trim();
            Mark = mark;
        }

        public abstract bool IsAutoGradable { get; }
        public abstract int AutoGrade(object? answer); // returns mark awarded (0..Mark)

        public override string ToString() => $"Q#{Id} [{GetType().Name}] ({Mark}): {Text}";
    }

    public class McqQuestion : Question
    {
        public IReadOnlyList<string> Options => _options;
        private readonly List<string> _options;
        public int CorrectIndex { get; }

        public McqQuestion(string text, int mark, IEnumerable<string> options, int correctIndex)
            : base(text, mark)
        {
            _options = options?.Select(o => o?.Trim() ?? "").Where(o => o.Length > 0).ToList()
                       ?? throw new ArgumentException("Options required.");
            if (_options.Count < 2) throw new ArgumentException("At least 2 options required.");
            if (correctIndex < 0 || correctIndex >= _options.Count) throw new ArgumentOutOfRangeException(nameof(correctIndex));
            CorrectIndex = correctIndex;
        }

        public override bool IsAutoGradable => true;

        // answer expected: int (index) or string (option text)
        public override int AutoGrade(object? answer)
        {
            if (answer is int idx) return idx == CorrectIndex ? Mark : 0;
            if (answer is string s)
            {
                var i = _options.FindIndex(o => string.Equals(o, s.Trim(), StringComparison.OrdinalIgnoreCase));
                return i == CorrectIndex ? Mark : 0;
            }
            return 0;
        }
    }

    public class TrueFalseQuestion : Question
    {
        public bool Correct { get; }

        public TrueFalseQuestion(string text, int mark, bool correct)
            : base(text, mark) => Correct = correct;

        public override bool IsAutoGradable => true;

        // answer expected: bool or "true"/"false"
        public override int AutoGrade(object? answer)
        {
            bool parsed;
            if (answer is bool b) parsed = b;
            else if (answer is string s && bool.TryParse(s, out var bb)) parsed = bb;
            else return 0;
            return parsed == Correct ? Mark : 0;
        }
    }

    public class EssayQuestion : Question
    {
        public EssayQuestion(string text, int mark) : base(text, mark) { }
        public override bool IsAutoGradable => false;
        public override int AutoGrade(object? answer) => 0; 
    }

    // ===================== Domain: Exam & Attempts =====================
    public class Exam
    {
        private static int _seq = 0;
        public int Id { get; } = ++_seq;
        public string Title { get; private set; }
        public Course Course { get; }
        public Instructor CreatedBy { get; }
        private readonly List<Question> _questions = new();
        public IReadOnlyList<Question> Questions => _questions.AsReadOnly();

        public bool IsStarted { get; private set; }
        public bool IsClosed { get; private set; }

        public Exam(string title, Course course, Instructor creator)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title required.");
            Title = title.Trim();
            Course = course ?? throw new ArgumentNullException(nameof(course));
            CreatedBy = creator ?? throw new ArgumentNullException(nameof(creator));
        }

        public int TotalMarks => _questions.Sum(q => q.Mark);

        private void EnsureModifiable()
        {
            if (IsStarted) throw new InvalidOperationException("Cannot modify exam after it starts.");
            if (IsClosed) throw new InvalidOperationException("Exam is closed.");
        }

        public void AddQuestion(Question q)
        {
            EnsureModifiable();
            if (q == null) throw new ArgumentNullException(nameof(q));
            if (TotalMarks + q.Mark > Course.MaxDegree)
                throw new InvalidOperationException("Adding this question exceeds course maximum degree.");
            _questions.Add(q);
        }

        public void RemoveQuestion(int questionId)
        {
            EnsureModifiable();
            var q = _questions.FirstOrDefault(x => x.Id == questionId)
                ?? throw new KeyNotFoundException("Question not found.");
            _questions.Remove(q);
        }

        public void EditTitle(string newTitle)
        {
            EnsureModifiable();
            if (string.IsNullOrWhiteSpace(newTitle)) throw new ArgumentException("Title required.");
            Title = newTitle.Trim();
        }

        public void Start()
        {
            if (IsStarted) return;
            if (_questions.Count == 0) throw new InvalidOperationException("Cannot start empty exam.");
            IsStarted = true;
        }

        public void Close()
        {
            if (!IsStarted) throw new InvalidOperationException("Exam hasn't started.");
            IsClosed = true;
        }

        public Exam DuplicateForCourse(Course targetCourse, Instructor by)
        {
            if (targetCourse == null) throw new ArgumentNullException(nameof(targetCourse));
            var copy = new Exam($"{Title} (Copy for {targetCourse.Title})", targetCourse, by);
            foreach (var q in _questions)
            {
                // shallow copy logic (new instances to keep independence)
                switch (q)
                {
                    case McqQuestion m:
                        copy.AddQuestion(new McqQuestion(m.Text, m.Mark, m.Options, m.CorrectIndex));
                        break;
                    case TrueFalseQuestion t:
                        copy.AddQuestion(new TrueFalseQuestion(t.Text, t.Mark, t.Correct));
                        break;
                    case EssayQuestion e:
                        copy.AddQuestion(new EssayQuestion(e.Text, e.Mark));
                        break;
                }
            }
            return copy;
        }
    }

    public class StudentAttempt
    {
        public Student Student { get; }
        public Exam Exam { get; }
        public DateTime StartedAt { get; } = DateTime.Now;
        public DateTime? SubmittedAt { get; private set; }

        // QuestionId -> Answer (object)
        private readonly Dictionary<int, object?> _answers = new();
        // Manual grades (e.g., Essay) QuestionId -> mark award
        private readonly Dictionary<int, int> _manualGrades = new();

        public StudentAttempt(Student s, Exam e)
        {
            Student = s ?? throw new ArgumentNullException(nameof(s));
            Exam = e ?? throw new ArgumentNullException(nameof(e));
            if (!e.IsStarted) throw new InvalidOperationException("Exam hasn't started yet.");
        }

        private void EnsureNotSubmitted()
        {
            if (SubmittedAt.HasValue) throw new InvalidOperationException("Attempt already submitted.");
        }

        public void Answer(int questionId, object? answer)
        {
            EnsureNotSubmitted();
            if (!Exam.Questions.Any(q => q.Id == questionId))
                throw new ArgumentException("Question doesn't belong to this exam.");
            _answers[questionId] = answer;
        }

        public void Submit() => SubmittedAt = DateTime.Now;

        public void ManualGrade(int questionId, int awardedMark)
        {
            if (!SubmittedAt.HasValue) throw new InvalidOperationException("Submit before manual grading.");
            var q = Exam.Questions.FirstOrDefault(x => x.Id == questionId)
                ?? throw new KeyNotFoundException("Question not found.");
            if (q.IsAutoGradable) throw new InvalidOperationException("This question is auto-gradable.");
            if (awardedMark < 0 || awardedMark > q.Mark) throw new ArgumentOutOfRangeException(nameof(awardedMark));
            _manualGrades[questionId] = awardedMark;
        }

        public int Score()
        {
            int total = 0;

            foreach (var q in Exam.Questions)
            {
                if (q.IsAutoGradable)
                {
                    _answers.TryGetValue(q.Id, out var ans);
                    total += q.AutoGrade(ans);
                }
                else
                {
                    if (_manualGrades.TryGetValue(q.Id, out int m)) total += m;
                }
            }
            return total;
        }
    }

    // ===================== Persistence: In-Memory Store =====================
    public class InMemoryDb
    {
        public List<Course> Courses { get; } = new();
        public List<Student> Students { get; } = new();
        public List<Instructor> Instructors { get; } = new();
        public List<Exam> Exams { get; } = new();
        public List<StudentAttempt> Attempts { get; } = new();

        public Course AddCourse(string title, string desc, int max) { var c = new Course(title, desc, max); Courses.Add(c); return c; }
        public Student AddStudent(string name, string email) { var s = new Student(name, email); Students.Add(s); return s; }
        public Instructor AddInstructor(string name, string spec) { var i = new Instructor(name, spec); Instructors.Add(i); return i; }

        public Exam AddExam(Exam e) { Exams.Add(e); return e; }
        public StudentAttempt AddAttempt(StudentAttempt a) { Attempts.Add(a); return a; }

        public IEnumerable<(string ExamTitle, string StudentName, string CourseName, int Score, bool Pass)> ExamReport(Exam exam, double passPercent = 60.0)
        {
            int max = exam.Course.MaxDegree;
            var attempts = Attempts.Where(a => a.Exam.Id == exam.Id);
            foreach (var a in attempts)
            {
                int score = a.Score();
                bool pass = score >= Math.Ceiling(max * (decimal)(passPercent / 100.0));
                yield return (exam.Title, a.Student.Name, exam.Course.Title, score, pass);
            }
        }

        public (Student s1, int s1Score, Student s2, int s2Score, string winner) CompareStudents(Exam exam, Student s1, Student s2)
        {
            int s1Score = Attempts.Where(a => a.Exam.Id == exam.Id && a.Student.Id == s1.Id).Select(a => a.Score()).DefaultIfEmpty(0).Max();
            int s2Score = Attempts.Where(a => a.Exam.Id == exam.Id && a.Student.Id == s2.Id).Select(a => a.Score()).DefaultIfEmpty(0).Max();
            string winner = s1Score == s2Score ? "Tie" : (s1Score > s2Score ? s1.Name : s2.Name);
            return (s1, s1Score, s2, s2Score, winner);
        }
    }

    // ===================== Demo / Main =====================
    class Program
    {
        static void Main()
        {
            var db = new InMemoryDb();

            // 1) Core entities
            var csharp = db.AddCourse("C# Fundamentals", "Intro to C#", max: 100);
            var oop = db.AddCourse("OOP", "OOP Principles", max: 80);

            var inst = db.AddInstructor("Karim Essam", "Software Engineering");
            inst.AssignTo(csharp);
            inst.AssignTo(oop);

            var s1 = db.AddStudent("Hagar Atia", "Hagar@example.com");
            var s2 = db.AddStudent("Shams Atia", "Shams@example.com");
            s1.Enroll(csharp); s2.Enroll(csharp);

            // 2) Build Exam for C#
            var exam1 = new Exam("C# Midterm", csharp, inst);
            db.AddExam(exam1);

            // Add questions (ensuring total <= MaxDegree)
            exam1.AddQuestion(new McqQuestion(
                "Which keyword defines a method that belongs to the class rather than an instance?",
                10, new[] { "virtual", "override", "static", "sealed" }, correctIndex: 2));
            exam1.AddQuestion(new TrueFalseQuestion("int is a reference type in C#.", 10, correct: false));
            exam1.AddQuestion(new EssayQuestion("Explain the difference between ref and out.", 20)); // manual grade

            // 3) Start exam (now modifications are locked)
            exam1.Start();

            // 4) Students take exam
            var a1 = new StudentAttempt(s1, exam1);
            a1.Answer(exam1.Questions[0].Id, 2);          
            a1.Answer(exam1.Questions[1].Id, false);       
            a1.Answer(exam1.Questions[2].Id, "ref needs init; out must be assigned inside method");
            a1.Submit();
            db.AddAttempt(a1);

            var a2 = new StudentAttempt(s2, exam1);
            a2.Answer(exam1.Questions[0].Id, "virtual");  
            a2.Answer(exam1.Questions[1].Id, "true");     
            a2.Answer(exam1.Questions[2].Id, "They both pass by reference but differ in initialization.");
            a2.Submit();
            db.AddAttempt(a2);

            // Manual grading for Essay
            a1.ManualGrade(exam1.Questions[2].Id, awardedMark: 16); 
            a2.ManualGrade(exam1.Questions[2].Id, awardedMark: 12);

            // 5) Report
            Console.WriteLine($"=== Report: {exam1.Title} ({exam1.Course}) ===");
            foreach (var r in db.ExamReport(exam1, passPercent: 60))
                Console.WriteLine($"Student: {r.StudentName,-12} | Score: {r.Score,3}/{exam1.Course.MaxDegree} | {(r.Pass ? "PASS" : "FAIL")}");

            // 6) Compare two students
            var cmp = db.CompareStudents(exam1, s1, s2);
            Console.WriteLine($"\nCompare: {cmp.s1.Name}({cmp.s1Score}) vs {cmp.s2.Name}({cmp.s2Score}) => Result: {cmp.winner}");

            // 7) Duplicate the exam for another course (OOP)
            var exam2 = exam1.DuplicateForCourse(oop, inst);
            db.AddExam(exam2);
            Console.WriteLine($"\nDuplicated '{exam1.Title}' to course '{oop.Title}' as '{exam2.Title}'");
            Console.WriteLine($"Exam2 total marks: {exam2.TotalMarks}/{oop.MaxDegree}");

            // Try to modify after start (should be blocked):
            try
            {
                exam1.AddQuestion(new TrueFalseQuestion("Will this be added?", 5, true));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Guard] Cannot modify started exam: {ex.Message}");
            }

           
            exam1.Close();

            Console.WriteLine("\nDone. Press any key to exit...");
            Console.ReadKey();
        }
    }
}
