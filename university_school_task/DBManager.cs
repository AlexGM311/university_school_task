using System;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using Org.BouncyCastle.Asn1.X509;

namespace university_school_task.DBManager
{
	public enum ReturnCode : ushort
	{
		Success = 0,
		InternalError = 1,
		SqlError = 2,
	}

	public class ExceptionManager
	{
		public ReturnCode code;
		public string message;

		public ExceptionManager(ReturnCode code, string message) => (this.code, this.message) = (code, message);

		public static implicit operator ExceptionManager(ReturnCode code) => new ExceptionManager(code, "");
	}

	public class ExceptionManager<T> : ExceptionManager
	{
		public ExceptionManager(ReturnCode code, string message) : base(code, message) { }

		public static implicit operator KeyValuePair<ExceptionManager, T?>(ExceptionManager<T> emg)
			=> new KeyValuePair<ExceptionManager, T?>(new ExceptionManager(emg.code, emg.message), default);

		public static implicit operator ExceptionManager<T>(ReturnCode code) => new ExceptionManager<T>(code, "");
	}

	public class Manager
	{
		/* DB Structure
			Students(
				uid INT PRIMARY KEY AUTO_INCREMENT,
				name VARCHAR(32) NOT NULL,
				surname VARCHAR(32) NOT NULL,
				department VARCHAR(32) NOT NULL,
				date_of_birth DATETIME DEFAULT CURRENT_TIMESTAMP
			);
			Teachers(
				uid INT PRIMARY KEY AUTO_INCREMENT,
				name VARCHAR(32) NOT NULL,
				surname VARCHAR(32) NOT NULL,
				department VARCHAR(32) NOT NULL
			);
			Courses(
				uid INT PRIMARY KEY AUTO_INCREMENT,
				title VARCHAR(32) NOT NULL,
				description VARCHAR(160) NOT NULL,
				teacher_id INT,
				FOREIGN KEY (teacher_id) REFERENCES Teachers(uid)
			);
			Exams(
				uid INT PRIMARY KEY AUTO_INCREMENT,
				date DATETIME DEFAULT CURRENT_TIMESTAMP,
				course_id INT,
				max_score INT,
				FOREIGN KEY (course_id) REFERENCES Courses(uid)
			);
			Grades(
				uid INT PRIMARY KEY AUTO_INCREMENT,
				student_id INT,
				exam_id INT,
				score INT,
				FOREIGN KEY (student_id) REFERENCES Students(uid),
				FOREIGN KEY (exam_id) REFERENCES Exams(uid)
			);*/

		public MySqlConnection connection { get; private set; }

		public Manager(MySqlConnection connection)
		{  
			this.connection = connection;
		}

		public void CreateDB()
		{
			List<string> args = new List<string>();

			args.Clear();
			args.Add("uid INT PRIMARY KEY AUTO_INCREMENT");
			args.Add("name VARCHAR(32) NOT NULL");
			args.Add("surname VARCHAR(32) NOT NULL");
			args.Add("department VARCHAR(32) NOT NULL");
			args.Add("date_of_birth DATETIME DEFAULT CURRENT_TIMESTAMP");
			CreateTable("Students", args);

			args.Clear();
			args.Add("uid INT PRIMARY KEY AUTO_INCREMENT");
			args.Add("name VARCHAR(32) NOT NULL");
			args.Add("surname VARCHAR(32) NOT NULL");
			args.Add("department VARCHAR(32) NOT NULL");
			CreateTable("Teachers", args);
			
			args.Clear();
			args.Add("uid INT PRIMARY KEY AUTO_INCREMENT");
			args.Add("title VARCHAR(32) NOT NULL");
			args.Add("description VARCHAR(160) NOT NULL");
			args.Add("teacher_id INT");
			args.Add("FOREIGN KEY (teacher_id) REFERENCES Teachers(uid) ON DELETE CASCADE");
			CreateTable("Courses", args);
			
			args.Clear();
			args.Add("uid INT PRIMARY KEY AUTO_INCREMENT");
			args.Add("date DATETIME DEFAULT CURRENT_TIMESTAMP");
			args.Add("course_id INT");
			args.Add("max_score INT");
			args.Add("FOREIGN KEY (course_id) REFERENCES Courses(uid) ON DELETE CASCADE");
			CreateTable("Exams", args);

			args.Clear();
			args.Add("uid INT PRIMARY KEY AUTO_INCREMENT");
			args.Add("student_id INT");
			args.Add("exam_id INT");
			args.Add("score INT");
			args.Add("FOREIGN KEY (student_id) REFERENCES Students(uid) ON DELETE CASCADE");
			args.Add("FOREIGN KEY (exam_id) REFERENCES Exams(uid) ON DELETE CASCADE");
			CreateTable("Grades", args);
		}
		
		#region Internal helper functions
		private string Wrap(string text) => "'" + text + "'";

		private void CreateTable(string tableName, List<string> tableRows)
		{
			using (var command = new MySqlCommand("", connection))
			using (var transaction = connection.BeginTransaction())
			{
				command.Transaction = transaction;
				command.CommandText = $"CREATE TABLE IF NOT EXISTS {tableName} (\n	{String.Join(",\n	", tableRows)}\n);";
				command.ExecuteNonQuery();
				command.Transaction.Commit();
				Console.WriteLine($"Таблица {tableName} успешно создана.");
			}
		}

		private ExceptionManager Insert(string tableName, List<string> columns, List<string> values)
		{
			try
			{
				using (var command = new MySqlCommand("", connection))
				using (var transaction = connection.BeginTransaction())
				{
					command.Transaction = transaction;
					command.CommandText = $"INSERT INTO {tableName} ({String.Join(", ", columns)}) VALUES ({String.Join(", ", values)});";
					// INSERT INTO name (column names) VALUES (column values);
					command.ExecuteNonQuery();
					command.Transaction.Commit();
				}
				return ReturnCode.Success;
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DbException:
						return new ExceptionManager(ReturnCode.SqlError, e.Message);
					default:
						return new ExceptionManager(ReturnCode.InternalError, e.Message);
				}
			}
		}
		
		private ExceptionManager Update(string tableName, int uid, Dictionary<string, string> updateInfo)
		{
			try
			{
				using (var command = new MySqlCommand("", connection))
				using (var transaction = connection.BeginTransaction())
				{
					command.Transaction = transaction;
					command.CommandText = $"UPDATE {tableName} SET {String.Join(", ", updateInfo.Select(kv => $"{kv.Key} = '{kv.Value}'"))} WHERE uid={uid};";
					/*
					UPDATE table_name
					SET column1 = value1, column2 = value2, ...
					WHERE uid=...;
					 */
					command.ExecuteNonQuery();
					command.Transaction.Commit();
				}
				return ReturnCode.Success;
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DbException:
						return new ExceptionManager(ReturnCode.SqlError, e.Message);
					default:
						return new ExceptionManager(ReturnCode.InternalError, e.Message);
				}
			}
		}

		private ExceptionManager Delete(string tableName, int uid)
		{
			try
			{
				using (var command = new MySqlCommand("", connection))
				using (var transaction = connection.BeginTransaction())
				{
					command.Transaction = transaction;
					command.CommandText = $"DELETE FROM {tableName} WHERE uid={uid};";
					// DELETE FROM tableName WHERE uid=...
					command.ExecuteNonQuery();
					command.Transaction.Commit();
				}
				return ReturnCode.Success;
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DbException:
						return new ExceptionManager(ReturnCode.SqlError, e.Message);
					default:
						return new ExceptionManager(ReturnCode.InternalError, e.Message);
				}
			}
		}
		#endregion

		#region Insertions
		public ExceptionManager AddStudent(string studentName, string studentSurname, string departament, string dateOfBirth)
			=>  Insert("Students", new List<string> { "name", "surname", "department", "date_of_birth" }, 
								   new List<string> { Wrap(studentName), Wrap(studentSurname), Wrap(departament), Wrap(dateOfBirth) });

		public ExceptionManager AddTeacher(string teacherName, string teacherSurname, string department)
			=> Insert("Teachers", new List<string> { "name", "surname", "department" },
								  new List<string> { Wrap(teacherName), Wrap(teacherSurname), Wrap(department) });

		public ExceptionManager AddCourse(string title, string description, int teacher_id)
			=> Insert("Courses", new List<string> { "title", "description", "teacher_id" },
								 new List<String> { Wrap(title), Wrap(description), teacher_id.ToString() });

		public ExceptionManager AddExam(string date, int course_id, int max_score)
			=> Insert("Exams", new List<string> { "date", "course_id", "max_score" },
							   new List<string> { date, course_id.ToString(), max_score.ToString() });

		public ExceptionManager AddGrade(int student_id, int exam_id, int score)
			=> Insert("Grades", new List<string> { "student_id", "exam_id", "score"},
								new List<string> { student_id.ToString(), exam_id.ToString(), score.ToString() });
		#endregion

		#region UpdateRows
		public ExceptionManager UpdateStudent(int uid, string? name = null, string? surname = null, string? departament = null, string? date_of_birth = null)
		{
			#pragma warning disable 
			var updateData = new Dictionary<string, string>()
			{
				{ nameof(name), name },
				{ nameof(surname), surname },
				{ nameof(departament), departament },
				{ nameof(date_of_birth), date_of_birth }
			} 
			#pragma warning restore
			.Where(kv => kv.Value != null)
			.ToDictionary(kv => kv.Key, kv => kv.Value!);

			return Update("Students", uid, updateData);
		}

		public ExceptionManager UpdateTeacher(int uid, string? name = null, string? surname = null, string? departament = null)
		{
			#pragma warning disable
			var updateData = new Dictionary<string, string>()
			{
				{ nameof(name), name },
				{ nameof(surname), surname },
				{ nameof(departament), departament },
			}
			#pragma warning restore
			.Where(kv => kv.Value != null)
			.ToDictionary(kv => kv.Key, kv => kv.Value!);

			return Update("Teachers", uid, updateData);
		}

		public ExceptionManager UpdateCourse(int uid, string? title = null, string? description = null, int? teacher_id = null)
		{
			string? tid = teacher_id?.ToString();
			#pragma warning disable
			var updateData = new Dictionary<string, string>()
			{
				{ nameof(title), title },
				{ nameof(description), description },
				{ nameof(teacher_id), tid },
			}
			#pragma warning restore
			.Where(kv => kv.Value != null)
			.ToDictionary(kv => kv.Key, kv => kv.Value!);

			return Update("Courses", uid, updateData);
		}

		public ExceptionManager UpdateExam(int uid, string? date = null, int? course_id = null, int? max_score = null)
		{
			string? cid = course_id?.ToString();
			string? mscore = max_score?.ToString();
			#pragma warning disable
			var updateData = new Dictionary<string, string>()
			{
				{ nameof(date), date },
				{ nameof(course_id), cid },
				{ nameof(max_score), mscore },
			}
			#pragma warning restore
			.Where(kv => kv.Value != null)
			.ToDictionary(kv => kv.Key, kv => kv.Value!);

			return Update("Exams", uid, updateData);
		}

		public ExceptionManager UpdateGrade(int uid, int? student_id = null, int? exam_id = null, int? score = null)
		{
			string? sid = student_id?.ToString();
			string? eid = exam_id?.ToString();
			string? sscore = score?.ToString();
			#pragma warning disable
			var updateData = new Dictionary<string, string>()
			{
				{ nameof(student_id), sid },
				{ nameof(exam_id), eid },
				{ nameof(score), sscore },
			}
			#pragma warning restore
			.Where(kv => kv.Value != null)
			.ToDictionary(kv => kv.Key, kv => kv.Value!);

			return Update("Grades", uid, updateData);
		}
		#endregion

		#region Removals
		public ExceptionManager DeleteStudent(int uid)
			=> Delete("Students", uid);

		public ExceptionManager DeleteTeacher(int uid)
			=> Delete("Teachers", uid);

		public ExceptionManager DeleteCourse(int uid)
			=> Delete("Courses", uid);

		public ExceptionManager DeleteExam(int uid)
			=> Delete("Exams", uid);

		public ExceptionManager DeleteGrade(int uid)
			=> Delete("Grades", uid);
		#endregion

		#region Other functions 
		public KeyValuePair<ExceptionManager, List<List<string>>?> StudentsByDepartment(string department)
		{
			try
			{
				var matrix = new List<List<string>>();
				using (var command = new MySqlCommand("", connection))
				{
					command.CommandText = $"SELECT uid, name, surname FROM Students WHERE department='{department}';";
					// SELECT uid, name, surname FROM Students WHERE department=...

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var row = new List<string>
							{
								reader["uid"].ToString() ?? "",
								reader["name"].ToString() ?? "",
								reader["surname"].ToString() ?? ""
							};

							matrix.Add(row);
						}
					}
					
				}
				return new KeyValuePair<ExceptionManager, List<List<string>>?>((ExceptionManager)ReturnCode.Success, matrix);
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DbException:
						return new ExceptionManager<List<List<string>>>(ReturnCode.SqlError, e.Message);
					default:
						return new ExceptionManager<List<List<string>>>(ReturnCode.InternalError, e.Message);
				}
			}
		}

		public KeyValuePair<ExceptionManager, List<List<string>>?> StudentsByCourse(string course)
		{
			try
			{
				var matrix = new List<List<string>>();
				using (var command = new MySqlCommand("", connection))
				{
					command.CommandText = $"SELECT Students.uid, name, surname FROM Students JOIN Grades ON Grades.student_id = Students.uid JOIN Exams ON Exams.uid = Grades.exam_id JOIN Courses ON Exams.course_id = Courses.uid WHERE Courses.title = '{course}';";

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var row = new List<string>
							{
								reader["uid"].ToString() ?? "",
								reader["name"].ToString() ?? "",
								reader["surname"].ToString() ?? ""
							};

							matrix.Add(row);
						}
					}

				}
				return new KeyValuePair<ExceptionManager, List<List<string>>?>((ExceptionManager)ReturnCode.Success, matrix);
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DbException:
						return new ExceptionManager<List<List<string>>>(ReturnCode.SqlError, e.Message);
					default:
						return new ExceptionManager<List<List<string>>>(ReturnCode.InternalError, e.Message);
				}
			}
		}

		public KeyValuePair<ExceptionManager, List<List<string>>?> CoursesByTeacher(int uid)
		{
			try
			{
				var matrix = new List<List<string>>();
				using (var command = new MySqlCommand("", connection))
				{
					command.CommandText = $"SELECT uid, title, description FROM Courses WHERE teacher_id = {uid};";
					// SELECT uid, title, description FROM Courses WHERE teacher_id = ...;

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var row = new List<string>
							{
								reader["uid"].ToString() ?? "",
								reader["title"].ToString() ?? "",
								reader["description"].ToString() ?? ""
							};

							matrix.Add(row);
						}
					}

				}
				return new KeyValuePair<ExceptionManager, List<List<string>>?>((ExceptionManager)ReturnCode.Success, matrix);
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DbException:
						return new ExceptionManager<List<List<string>>>(ReturnCode.SqlError, e.Message);
					default:
						return new ExceptionManager<List<List<string>>>(ReturnCode.InternalError, e.Message);
				}
			}
		}

		public KeyValuePair<ExceptionManager, List<List<string>>?> CoursesByTeacher(string name, string surname)
		{
			try
			{
				var matrix = new List<List<string>>();
				using (var command = new MySqlCommand("", connection))
				{
					command.CommandText = $"SELECT Courses.uid, title, description FROM Teachers JOIN Courses ON teacher_id=Teachers.uid WHERE name = '{name}' AND surname = '{surname}';";
					/*
					 SELECT 
						Courses.uid, 
						title, 
						description 
					FROM 
						Teachers 
					JOIN 
						Courses ON teacher_id = Teachers.uid 
					WHERE 
						name = ... 
						AND surname = ...;
					 */

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var row = new List<string>
							{
								reader["uid"].ToString() ?? "",
								reader["title"].ToString() ?? "",
								reader["description"].ToString() ?? ""
							};

							matrix.Add(row);
						}
					}

				}
				return new KeyValuePair<ExceptionManager, List<List<string>>?>((ExceptionManager)ReturnCode.Success, matrix);
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DbException:
						return new ExceptionManager<List<List<string>>>(ReturnCode.SqlError, e.Message);
					default:
						return new ExceptionManager<List<List<string>>>(ReturnCode.InternalError, e.Message);
				}
			}
		}

		public KeyValuePair<ExceptionManager, List<List<string>>?> GradesByCourse(int course_id)
		{
			try
			{
				var matrix = new List<List<string>>();
				using (var command = new MySqlCommand("", connection))
				{
					command.CommandText = $"SELECT Grades.uid, score FROM Grades JOIN Exams on exam_id = Exams.uid WHERE course_id = {course_id};";
					// SELECT Grades.uid, score FROM Grades JOIN Exams on exam_id = Exams.uid WHERE course_id = ...;

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var row = new List<string>
							{
								reader["uid"].ToString() ?? "",
								reader["score"].ToString() ?? "",
							};

							matrix.Add(row);
						}
					}

				}
				return new KeyValuePair<ExceptionManager, List<List<string>>?>((ExceptionManager)ReturnCode.Success, matrix);
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DbException:
						return new ExceptionManager<List<List<string>>>(ReturnCode.SqlError, e.Message);
					default:
						return new ExceptionManager<List<List<string>>>(ReturnCode.InternalError, e.Message);
				}
			}
		}

		public KeyValuePair<ExceptionManager, List<List<string>>?> GradesByCourse(string title)
		{
			try
			{
				var matrix = new List<List<string>>();
				using (var command = new MySqlCommand("", connection))
				{
					command.CommandText = $"SELECT Grades.uid, score FROM Courses JOIN Exams ON Courses.uid = course_id JOIN Grades ON exam_id = Exams.uid WHERE title = '{title}';";
					/*
					 SELECT 
						Grades.uid, score 
					FROM 
						Courses 
					JOIN 
						Exams ON Courses.uid = course_id 
					JOIN
						Grades ON exam_id = Exams.uid
					WHERE
						title = '...';
					 */

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var row = new List<string>
							{
								reader["uid"].ToString() ?? "",
								reader["score"].ToString() ?? "",
							};

							matrix.Add(row);
						}
					}

				}
				return new KeyValuePair<ExceptionManager, List<List<string>>?>((ExceptionManager)ReturnCode.Success, matrix);
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DbException:
						return new ExceptionManager<List<List<string>>>(ReturnCode.SqlError, e.Message);
					default:
						return new ExceptionManager<List<List<string>>>(ReturnCode.InternalError, e.Message);
				}
			}
		}

		public KeyValuePair<ExceptionManager, List<KeyValuePair<int, double>>?> AverageStudentMark(int student_id)
		{
			try 
			{ 
				var matrix = new List<KeyValuePair<int, double>>();
				using (var command = new MySqlCommand("", connection))
				{
					command.CommandText = $"SELECT Students.uid as 'student_id', avg(score) as score FROM Grades WHERE student_id = {student_id} GROUP BY student_id;";
					// SELECT Grades.uid, score FROM Grades JOIN Exams on exam_id = Exams.uid WHERE course_id = ...;

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var row = new KeyValuePair<int, double>(
								int.Parse(reader["student_id"].ToString() ?? "0"),
								Double.Parse(reader["score"].ToString() ?? "0.0")
							);

							matrix.Add(row);
						}
					}
				}
				return new KeyValuePair<ExceptionManager, List<KeyValuePair<int, double>>?> ((ExceptionManager)ReturnCode.Success, matrix);
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DbException:
						return new ExceptionManager<List<KeyValuePair<int, double>>?>(ReturnCode.SqlError, e.Message);
					default:
						return new ExceptionManager<List<KeyValuePair<int, double>>?>(ReturnCode.InternalError, e.Message);
				}
			}
		}

		public KeyValuePair<ExceptionManager, List<KeyValuePair<int, double>>?> AverageStudentMark(string name, string surname)
		{
			try
			{
				var matrix = new List<KeyValuePair<int, double>>();
				using (var command = new MySqlCommand("", connection))
				{
					command.CommandText = $"SELECT Students.uid as 'student_id', avg(score) as score FROM Grades JOIN Students ON student_id = Students.uid WHERE  name = '{name}' AND surname = '{surname}' GROUP BY student_id;";
					/*
					 SELECT 
						Students.uid as 'student_id',
						avg(score) as score 
					FROM 
						Grades 
					JOIN
						Students ON student_id = Students.uid
					WHERE 
						name = 'Michelle' AND surname = 'Singh'
					GROUP BY student_id;
					 */

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var row = new KeyValuePair<int, double>(
								int.Parse(reader["student_id"].ToString() ?? "0"),
								Double.Parse(reader["score"].ToString() ?? "0.0")
							);

							matrix.Add(row);
						}
					}
				}
				return new KeyValuePair<ExceptionManager, List<KeyValuePair<int, double>>?>((ExceptionManager)ReturnCode.Success, matrix);
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DbException:
						return new ExceptionManager<List<KeyValuePair<int, double>>?>(ReturnCode.SqlError, e.Message);
					default:
						return new ExceptionManager<List<KeyValuePair<int, double>>?>(ReturnCode.InternalError, e.Message);
				}
			}
		}

		public KeyValuePair<ExceptionManager, double?> AverageByCourse(string title)
		{
			try
			{
				double result = 0.0;
				using (var command = new MySqlCommand("", connection))
				{
					command.CommandText = $"SELECT avg(Score) as 'avg' FROM Grades JOIN Exams ON Exams.uid = Grades.exam_id JOIN Courses ON Courses.uid = Exams.course_id WHERE title = '{title}'";

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							result = Double.Parse(reader["avg"].ToString() ?? "0.0");
						}
					}
				}
				return new KeyValuePair<ExceptionManager, double?>((ExceptionManager)ReturnCode.Success, result);
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DbException:
						return new ExceptionManager<double?>(ReturnCode.SqlError, e.Message);
					default:
						return new ExceptionManager<double?>(ReturnCode.InternalError, e.Message);
				}
			}
		}

		public KeyValuePair<ExceptionManager, double?> AverageOnDepartment(string department)	
		{
			try
			{
				double? result = null;
				using (var command = new MySqlCommand("", connection))
				{
					command.CommandText = $"SELECT Students.department, avg(Grades.score) FROM Students JOIN Grades ON Students.uid = Grades.student_id WHERE department = '{department}' GROUP BY Students.department;";
					/*
					SELECT Students.department, avg(Grades.score) FROM Students 
					JOIN Grades ON Students.uid = Grades.student_id 
					WHERE department = ... 
					GROUP BY Students.department;
					*/
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							result = double.Parse(reader["score"].ToString() ?? "0.0");
						}
					}
				}
				return new KeyValuePair<ExceptionManager, double?>((ExceptionManager)ReturnCode.Success, result);
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DbException:
						return new ExceptionManager<double?>(ReturnCode.SqlError, e.Message);
					default:
						return new ExceptionManager<double?>(ReturnCode.InternalError, e.Message);
				}
			}
		}

		public ExceptionManager MassExecute(List<string> commands)
		{
			try
			{
				var command = new MySqlCommand("", connection);
				var transaction = connection.BeginTransaction();
				command.Transaction = transaction;
				foreach (var com in commands)
				{
					command.CommandText = com;
					command.ExecuteNonQuery();
				}
				command.Transaction.Commit();
				return ReturnCode.Success;
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DbException:
						return new ExceptionManager<double?>(ReturnCode.SqlError, e.Message);
					default:
						return new ExceptionManager<double?>(ReturnCode.InternalError, e.Message);
				}
			}
		}
		
		public KeyValuePair<ExceptionManager, string?> GetTable(string tableName)
		{
			try
			{
				var matrix = new List<string>();
				using (var command = new MySqlCommand("", connection))
				{
					command.CommandText = $"SELECT * FROM {tableName};";

					using (var reader = command.ExecuteReader())
					{
						var resultString = new StringBuilder();

						for (int i = 0; i < reader.FieldCount; i++)
						{
							resultString.Append(reader.GetName(i)).Append(i < reader.FieldCount - 1 ? ", " : "\n");
						}

						while (reader.Read())
						{
							for (int i = 0; i < reader.FieldCount; i++)
							{
								resultString.Append(reader.GetValue(i)?.ToString() ?? "NULL");
								if (i < reader.FieldCount - 1)
									resultString.Append(", ");
							}
							resultString.AppendLine();
						}

						return new KeyValuePair<ExceptionManager, string?>((ExceptionManager)ReturnCode.Success, resultString.ToString());
					}
				}
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DbException:
						return new ExceptionManager<string?>(ReturnCode.SqlError, e.Message);
					default:
						return new ExceptionManager<string?>(ReturnCode.InternalError, e.Message);
				}
			}
		}

		#endregion
	}
}
