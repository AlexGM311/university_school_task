using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Web;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto.Modes;
namespace university_school_task.DBManager;

static class Program
{
	static void CreateDatabaseIfNotExists(MySqlConnection connection, string dbName)
	{
		var commandText = $"CREATE DATABASE IF NOT EXISTS `{dbName}`;";

		using (var command = new MySqlCommand(commandText, connection))
		{
			command.ExecuteNonQuery(); // Execute the command
		}
	}

	static int Menu(int selectedIndex, ref int state, string[] options, string starting_line, out bool escape)
	{
		Console.Clear();
		Console.WriteLine(starting_line);
		escape = false;

		for (int i = 0; i < options.Length; i++)
		{
			if (i == selectedIndex)
			{
				Console.BackgroundColor = ConsoleColor.Gray;
				Console.ForegroundColor = ConsoleColor.Black;
			}

			Console.WriteLine(options[i]);

			Console.ResetColor();
		}

		var key = Console.ReadKey(true);

		if (key.Key == ConsoleKey.UpArrow)
		{
			selectedIndex = (selectedIndex > 0) ? selectedIndex - 1 : options.Length - 1;
		}
		else if (key.Key == ConsoleKey.DownArrow)
		{
			selectedIndex = (selectedIndex < options.Length - 1) ? selectedIndex + 1 : 0;
		}
		else if (key.Key == ConsoleKey.Enter)
		{
			state++;
		}
		else if (key.Key == ConsoleKey.Escape)
		{
			escape = true;
		}
		return selectedIndex;
	}
	
	static int MenuSelector(int selectedIndex, ref int state, string[] options, string starting_line, out bool escape, ref bool[] selectedIndexes)	
	{
		Console.Clear();
		Console.WriteLine(starting_line);
		escape = false;
		state = 0;
		for (int i = 0; i < options.Length; i++)
		{
			if (selectedIndexes[i])
			{
				Console.BackgroundColor = ConsoleColor.DarkBlue;
				Console.ForegroundColor = ConsoleColor.Green;
			}

			if (i == selectedIndex)
			{
				Console.BackgroundColor = ConsoleColor.Gray;
				Console.ForegroundColor = ConsoleColor.Black;
			}

			Console.WriteLine(options[i]);

			Console.ResetColor();
		}

		var key = Console.ReadKey(true);

		if (key.Key == ConsoleKey.Enter && (key.Modifiers & ConsoleModifiers.Control) != 0)
		{
			state = 1;
			return selectedIndex;
		}

		if (key.Key == ConsoleKey.UpArrow)
		{
			selectedIndex = (selectedIndex > 0) ? selectedIndex - 1 : options.Length - 1;
		}
		else if (key.Key == ConsoleKey.DownArrow)
		{
			selectedIndex = (selectedIndex < options.Length - 1) ? selectedIndex + 1 : 0;
		}
		else if (key.Key == ConsoleKey.Enter)
		{
			selectedIndexes[selectedIndex] = !selectedIndexes[selectedIndex];
		}
		else if (key.Key == ConsoleKey.Escape)
		{
			escape = true;
		}
		return selectedIndex;
	}

	static bool ReadLineWithEsc(out string input)
	{
		input = string.Empty;
		StringBuilder builder = new StringBuilder();
		int cursorPosition = 0;

		while (true)
		{
			var keyInfo = Console.ReadKey(intercept: true);

			if (keyInfo.Key == ConsoleKey.Escape)
			{
				input = string.Empty;
				return false;
			}
			else if (keyInfo.Key == ConsoleKey.Enter)
			{
				Console.WriteLine();
				input = builder.ToString();
				return true;
			}
			else if (keyInfo.Key == ConsoleKey.Backspace && cursorPosition > 0)
			{
				builder.Remove(cursorPosition - 1, 1);
				cursorPosition--;
				Console.Write("\b \b");

				// Перерисовка оставшейся части строки
				Console.Write(builder.ToString(cursorPosition, builder.Length - cursorPosition) + " ");
				Console.SetCursorPosition(Console.CursorLeft - (builder.Length - cursorPosition + 1), Console.CursorTop);
			}
			else if (keyInfo.Key == ConsoleKey.Delete && cursorPosition < builder.Length)
			{
				builder.Remove(cursorPosition, 1);

				// Перерисовка оставшейся части строки
				Console.Write(builder.ToString(cursorPosition, builder.Length - cursorPosition) + " ");
				Console.SetCursorPosition(Console.CursorLeft - (builder.Length - cursorPosition + 1), Console.CursorTop);
			}
			else if (keyInfo.Key == ConsoleKey.LeftArrow && cursorPosition > 0)
			{
				cursorPosition--;
				Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
			}
			else if (keyInfo.Key == ConsoleKey.RightArrow && cursorPosition < builder.Length)
			{
				cursorPosition++;
				Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
			}
			else if (!char.IsControl(keyInfo.KeyChar))
			{
				builder.Insert(cursorPosition, keyInfo.KeyChar);
				Console.Write(builder.ToString(cursorPosition, builder.Length - cursorPosition));
				cursorPosition++;

				// Перемещение курсора в конец введённой части
				Console.SetCursorPosition(Console.CursorLeft - (builder.Length - cursorPosition), Console.CursorTop);
			}
		}
	}

	static void Main()
	{
		string connectionString = "Server=localhost;port=3306;User ID=root;Password=noneedtobecomplex";
		using (var connection = new MySqlConnection(connectionString))
		{
			connection.Open();
			CreateDatabaseIfNotExists(connection, "university");

			using (var command = new MySqlCommand("USE university;", connection))
			{
				command.ExecuteNonQuery();
			}

			var dbManager = new DBManager.Manager(connection);
			dbManager.CreateDB();

			bool online = true;
			int selectedIndex = 0;
			int state = 0;

			string[] menuGeneral = {
				" 1. Добавление нового студента, преподавателя, курса, экзамена и оценки.",
				" 2. Изменение информации о студентах, преподавателях и курсах",
				" 3. Удаление студентов, преподавателей, курсов и экзаменов.",
				" 4. Получение списка студентов по факультету.",
				" 5. Получение списка курсов, читаемых определенным преподавателем",
				" 6. Получение списка студентов, зачисленных на конкретный курс",
				" 7. Получение оценок студентов по определенному курсу.",
				" 8. Средний балл студентов по определенному курсу.",
				" 9. Средний балл студента в целом.",
				"10. Средний балл по факультету",
				"11. Массовое выполнение SLQ команд",
				"12. Вывод таблицы",
				"13. Выход"
			};

			List<string> sqlCommands = new List<string>();
			bool escape;

			while (online)
			{
				selectedIndex = Menu(selectedIndex, ref state, menuGeneral, "Меню:", out escape);
				if (state == 1)
				{	switch (selectedIndex)
					{
						case 0:
							AddEntity(dbManager);
							break;
						case 1:
							UpdateEntity(dbManager);
							break;
						case 2:
							DeleteEntity(dbManager);
							break;
						case 3:
							QueryStudentsPerFaculty(dbManager);
							break;
						case 4:
							QueryCoursesByInstructor(dbManager);
							break;
						case 5:
							QueryStudentsByCourse(dbManager);
							break;
						case 6:
							QueryGradesByCourse(dbManager);
							break;
						case 7:
							CalculateAverageGradeByCourse(dbManager);
							break;
						case 8:
							CalculateOverallAverageGrade(dbManager);
							break;
						case 9:
							CalculateAverageGradeByFaculty(dbManager);
							break;
						case 10:
							MassExecute(dbManager);
							break;
						case 11:
							DisplayTable(dbManager);
							break;
						case 12:
							online = false;
							Console.WriteLine("Выход из программы...");
							break;
					}
				}
				state = 0;
			}
			connection.Close();
		}
	}

	static void cancelAction(string line)
	{
		Console.WriteLine("\n" + line + " Нажмите любую кнопку, чтобы вернуться в меню.");
		Console.ReadKey();
	}

	static void AddEntity(DBManager.Manager dbManager)
	{
		// Реализация добавления студента, преподавателя, курса и т.д.
		Console.WriteLine("Выберите тип сущности для добавления (Студент, Преподаватель и т.д.): ");
		int selected_index = 0;
		int state = 0;
		string[] menu_options =
		{
			"1. Студента",
			"2. Преподавателя",
			"3. Курс",
			"4. Экзамен",
			"5. Оценку"
		};
		bool shouldExit = false;
		

		// Метод для считывания строки с возможностью отмены
		bool PromptAndReadLine(string prompt, out string result)
		{
			Console.Write(prompt);
			if (!ReadLineWithEsc(out result))
			{
				cancelAction("Отмена добавления объекта в базу данных.");
				return false;
			}
			return true;
		}
		string name, surname, department, date_of_birth;
		int id, score, other_id;


		while (!shouldExit)
		{
			switch (state)
			{
				case 0:
					selected_index = Menu(selected_index, ref state, menu_options, "", out shouldExit);
					break;
				case 1:
					switch(selected_index)
					{
						case 0:
							// Запросы на ввод данных
							if (!PromptAndReadLine("Введите имя ученика: ", out name) ||
								!PromptAndReadLine("Введите фамилию ученика: ", out surname) ||
								!PromptAndReadLine("Введите факультет ученика: ", out department) ||
								!PromptAndReadLine("Введите дату рождения ученика: ", out date_of_birth))
							{
								return;
							}

							var transaction_result0 = dbManager.AddStudent(name, surname, department, date_of_birth);
							if (transaction_result0.code == ReturnCode.Success)
							{
								cancelAction("Ученик успешно внесён!");
								return;
                            }
							else
							{
								if (transaction_result0.code == ReturnCode.InternalError)
								{
                                    cancelAction("Что-то пошло не так.");

									return;
								}
								else
								{
									cancelAction("Ошибка в запросе.");
									return;
								}
							}
						case 1:
							// Запросы на ввод данных
							if (!PromptAndReadLine("Введите имя учителя: ", out name) ||
								!PromptAndReadLine("Введите фамилию учителя: ", out surname) ||
								!PromptAndReadLine("Введите факультет учителя: ", out department))
							{
								return;
							}

							var transaction_result1 = dbManager.AddTeacher(name, surname, department);
							if (transaction_result1.code == ReturnCode.Success)
							{
								cancelAction("Учитель успешно внесён!");
								return;
							}
							else
							{
								if (transaction_result1.code == ReturnCode.InternalError)
								{
									Console.WriteLine(transaction_result1.message);
									cancelAction("Что-то пошло не так.");
									return;
								}
								else
								{
									Console.WriteLine(transaction_result1.message);
									cancelAction("Ошибка в запросе.");
									return;
								}
							}
						case 2:
							// Запросы на ввод данных
							if (!PromptAndReadLine("Введите название курса: ", out name) ||
								!PromptAndReadLine("Введите описание курса: ", out surname) ||
								!PromptAndReadLine("Введите айди учителя, ведущего курс: ", out department))
							{
								return;
							}

							if (!int.TryParse(department, out id))
							{
								cancelAction("Неправильный формат ввода.");
								return;
							}

                            var transaction_result2 = dbManager.AddCourse(name, surname, id);
							if (transaction_result2.code == ReturnCode.Success)
							{
								cancelAction("Курс успешно внесён!");
								return;
							}
							else
							{
								if (transaction_result2.code == ReturnCode.InternalError)
								{
									cancelAction("Что-то пошло не так.");
									return;
								}
								else
								{
									cancelAction("Ошибка в запросе.");
									return;
								}
							}
						case 3:
							// Запросы на ввод данных
							if (!PromptAndReadLine("Введите идентификатор курса: ", out name) ||
								!PromptAndReadLine("Введите новую дату курса: ", out surname) ||
								!PromptAndReadLine("Введите максимальную оценку: ", out department))
							{
								return;
							}

							if (!int.TryParse(name, out id) ||
								!int.TryParse(department, out score))
							{
								cancelAction("Неправильный формат ввода.");
								return;
							}

							var transaction_result3 = dbManager.AddExam(surname, id, score);
							if (transaction_result3.code == ReturnCode.Success)
							{
								cancelAction("Экзамен успешно успешно создан!");
								return;
							}
							else
							{
								if (transaction_result3.code == ReturnCode.InternalError)
								{
									cancelAction("Что-то пошло не так.");
									return;
								}
								else
								{
									cancelAction("Ошибка в запросе.");
									return;
								}
							}
						case 4:
							// Запросы на ввод данных
							if (!PromptAndReadLine("Введите оценку: ", out name) ||
								!PromptAndReadLine("Введите идентификатор студента: ", out surname) ||
								!PromptAndReadLine("Введите индетификатор экзамена: ", out department))
							{
								return;
							}

							if (!int.TryParse(name, out score) ||
								!int.TryParse(surname, out id) ||
								!int.TryParse(department, out other_id))
							{
								cancelAction("Неправильный формат ввода.");
								return;
							}

							var transaction_result4 = dbManager.AddGrade(id, other_id, score);
							if (transaction_result4.code == ReturnCode.Success)
							{
								cancelAction("Оценка успешно добавлена!");
								return;
							}
							else
							{
								if (transaction_result4.code == ReturnCode.InternalError)
								{
									cancelAction("Что-то пошло не так.");
									return;
								}
								else
								{
									cancelAction("Ошибка в запросе.");
									return;
								}
							}
					}
					return;
			}
		}
	}

	static void UpdateEntity(DBManager.Manager dbManager)
	{
		// Реализация изменения информации о студентах, преподавателях и т.д.
		Console.WriteLine("Выберите тип сущности для изменения: ");
		int selected_index = 0, innerSelectedIndex = 0;
		int state = 0, innerState = 0;
		string[] menu_options =
		{
			"1. Студента",
			"2. Преподавателя",
			"3. Курс",
			"4. Экзамен",
			"5. Оценку"
		};
		bool shouldExit = false;
		bool[] options = new bool[0];

		// Метод для считывания строки с возможностью отмены
		bool PromptAndReadLine(string prompt, out string result)
		{
			Console.Write(prompt);
			if (!ReadLineWithEsc(out result))
			{
				cancelAction("Отмена изменения объекта в базе данных.");
				return false;
			}
			return true;
		}
		string[]? selectable;
		string?[] s = { null, null, null, null };
		string[] S = { "", "", "", "" };
		int?[] i = {null, null, null, null};
		int[] I = { -1, -1, -1, -1 , -1};
		DBManager.ExceptionManager transactionResult;

		while (!shouldExit)
		{
			
			switch (state)
			{
				case 0:
					selected_index = Menu(selected_index, ref state, menu_options, "", out shouldExit);
					break;
				case 1:
					switch (selected_index)
					{
						case 0:
							Array.Resize(ref options, 4);
							if (shouldExit)
								return;
							innerState = 0;
							selectable = new string[]
							{
								"Имя",
								"Фамилию",
								"Дату рождения",
								"Факультет"
							};
							while (!shouldExit && innerState == 0)
								innerSelectedIndex = MenuSelector(innerSelectedIndex, ref innerState, selectable, "Выберите все подходящие варианты, Ctrl+Enter чтобы завершить.", out shouldExit, ref options);

							if (shouldExit)
								return;
							if ((!PromptAndReadLine("Введите идентификатор изменяемого: ", out S[0])) ||
								(options[0] && !PromptAndReadLine("Введите новое имя ученика: ", out s[0])) ||
								(options[1] && !PromptAndReadLine("Введите новую фамилию ученика: ", out s[1])) ||
								(options[2] && !PromptAndReadLine("Введите новую дату рождения ученика: ", out s[2])) ||
								(options[3] && !PromptAndReadLine("Введите новый факультет ученика: ", out s[3])))
							{
								return;
							}

							if (!int.TryParse(S[0], out I[0]))
							{
								cancelAction("Неверный ввод!");
								return;
							}

							transactionResult = dbManager.UpdateStudent(I[0], s[0], s[1], s[2], s[3]);
							if (transactionResult.code == ReturnCode.Success)
							{
								cancelAction("Ученик успешно изменён!");
								return;
							}
							else
							{
								if (transactionResult.code == ReturnCode.InternalError)
								{
									cancelAction("Что-то пошло не так.");
									return;
								}
								else
								{
									cancelAction("Ошибка в запросе.");
									return;
								}
							}
						case 1:
							Array.Resize(ref options, 3);
							if (shouldExit)
								return;
							innerState = 0;
							selectable = new string[]
							{
								"Имя",
								"Фамилию",
								"Факультет"
							};
							while (!shouldExit && innerState == 0)
								innerSelectedIndex = MenuSelector(innerSelectedIndex, ref innerState, selectable, "Выберите все подходящие варианты, Ctrl+Enter чтобы завершить.", out shouldExit, ref options);

							if (shouldExit)
								return;
							if ((!PromptAndReadLine("Введите идентификатор изменяемого: ", out S[0])) ||
								(options[0] && !PromptAndReadLine("Введите новое имя учителя: ", out s[0])) ||
								(options[1] && !PromptAndReadLine("Введите новую фамилию учителя: ", out s[1])) ||
								(options[2] && !PromptAndReadLine("Введите новый факультет учителя: ", out s[2])))
							{
								return;
							}

							if (!int.TryParse(S[0], out I[0]))
							{
								cancelAction("Неверный ввод!");
								return;
							}

							transactionResult = dbManager.UpdateTeacher(I[0], s[0], s[1], s[2]);
							if (transactionResult.code == ReturnCode.Success)
							{
								cancelAction("Учитель успешно изменён!");
								return;
							}
							else
							{
								if (transactionResult.code == ReturnCode.InternalError)
								{
									cancelAction("Что-то пошло не так.");
									return;
								}
								else
								{
									cancelAction("Ошибка в запросе.");
									return;
								}
							}
						case 2:
							Array.Resize(ref options, 3);
							if (shouldExit)
								return;
							innerState = 0;
							selectable = new string[]
							{
								"Название",
								"Описание",
								"Ведущий учитель"
							};
							while (!shouldExit && innerState == 0)
								innerSelectedIndex = MenuSelector(innerSelectedIndex, ref innerState, selectable, "Выберите все подходящие варианты, Ctrl+Enter чтобы завершить.", out shouldExit, ref options);

							if (shouldExit)
								return;
							if ((!PromptAndReadLine("Введите идентификатор изменяемого курса: ", out S[0])) ||
								(options[0] && !PromptAndReadLine("Введите новое название курса: ", out s[0])) ||
								(options[1] && !PromptAndReadLine("Введите новое описание курса: ", out s[1])) ||
								(options[2] && !PromptAndReadLine("Введите индекс нового ведущего учителя: ", out s[2])))
							{
								return;
							}
							if (!int.TryParse(S[0], out I[0]))
							{
								cancelAction("Неверный ввод!");
								return;
							}

							if (s[2] != null)
							{
								if (!int.TryParse(s[2], out I[3]))
								{
									cancelAction("Неверный ввод!");
									return;
								}
								i[2] = I[3];
							}

							transactionResult = dbManager.UpdateCourse(I[0], s[0], s[1], i[2]);
							if (transactionResult.code == ReturnCode.Success)
							{
								cancelAction("Курс успешно изменён!");
								return;
							}
							else
							{
								if (transactionResult.code == ReturnCode.InternalError)
								{
									cancelAction("Что-то пошло не так.");
									return;
								}
								else
								{
									cancelAction("Ошибка в запросе.");
									return;
								}
							}
						case 3:
							Array.Resize(ref options, 3);
							if (shouldExit)
								return;
							innerState = 0;
							selectable = new string[]
							{
								"Дата",
								"Идентификатор курса",
								"Максимальная оценка"
							};
							while (!shouldExit && innerState == 0)
								innerSelectedIndex = MenuSelector(innerSelectedIndex, ref innerState, selectable, "Выберите все подходящие варианты, Ctrl+Enter чтобы завершить.", out shouldExit, ref options);

							if (shouldExit)
								return;
							if ((!PromptAndReadLine("Введите идентификатор изменяемого экзамена: ", out S[0])) ||
								(options[0] && !PromptAndReadLine("Введите новую дату экзамена: ", out s[0])) ||
								(options[1] && !PromptAndReadLine("Введите новый идентификатор курса: ", out s[1])) ||
								(options[2] && !PromptAndReadLine("Введите новый максимальный балл: ", out s[2])))
							{
								return;
							}
							if (!int.TryParse(S[0], out I[0]))
							{
								cancelAction("Неверный ввод!");
								return;
							}
							for (int j = 1; j < 3; j++)
							{
								if (s[j] != null)
								{
									if (!int.TryParse(s[j], out I[j + 1]))
									{
										cancelAction("Неверный ввод!");
										return;
									}
									i[j] = I[j + 1];
								}
							}

							transactionResult = dbManager.UpdateExam(I[0], s[0], i[1], i[2]);
							if (transactionResult.code == ReturnCode.Success)
							{
								cancelAction("Экзамен успешно изменён!");
								return;
							}
							else
							{
								if (transactionResult.code == ReturnCode.InternalError)
								{
									cancelAction("Что-то пошло не так.");
									return;
								}
								else
								{
									cancelAction("Ошибка в запросе.");
									return;
								}
							}
						case 4:
							Array.Resize(ref options, 3);
							if (shouldExit)
								return;
							innerState = 0;
							selectable = new string[]
							{
								"Идентификатор студента",
								"Идентификатор экзамена",
								"Оценку"
							};
							while (!shouldExit && innerState == 0)
								innerSelectedIndex = MenuSelector(innerSelectedIndex, ref innerState, selectable, "Выберите все подходящие варианты, Ctrl+Enter чтобы завершить.", out shouldExit, ref options);

							if (shouldExit)
								return;
							if ((!PromptAndReadLine("Введите идентификатор изменяемой оценки: ", out S[0])) ||
								(options[0] && !PromptAndReadLine("Введите новый идентификатор студента: ", out s[0])) ||
								(options[1] && !PromptAndReadLine("Введите новый идентификатор экзамена: ", out s[1])) ||
								(options[2] && !PromptAndReadLine("Введите новый балл: ", out s[2])))
							{
								return;
							}

							for (int j = 0; j < 3; j++)
							{
								if (s[j] != null)
								{
									if (!int.TryParse(s[j], out I[j + 1]))
									{
										cancelAction("Неверный ввод!");
										return;
									}
									i[j] = I[j + 1];
								}
							}

							var transaction_result4 = dbManager.UpdateGrade(I[0], i[0], i[1], i[2]);
							if (transaction_result4.code == ReturnCode.Success)
							{
								cancelAction("Оценка успешно изменена!");
								return;
							}
							else
							{
								if (transaction_result4.code == ReturnCode.InternalError)
								{
									cancelAction("Что-то пошло не так.");
									return;
								}
								else
								{
									cancelAction("Ошибка в запросе.");
									return;
								}
							}
					}
					return;
			}
		}
	}

	static void DeleteEntity(DBManager.Manager dbManager)
	{
		// Реализация добавления студента, преподавателя, курса и т.д.
		Console.WriteLine("Выберите тип сущности для добавления (Студент, Преподаватель и т.д.): ");
		int selected_index = 0;
		int state = 0;
		string[] menu_options =
		{
			"1. Студента",
			"2. Преподавателя",
			"3. Курс",
			"4. Экзамен",
			"5. Оценку"
		};
		bool shouldExit = false;


		// Метод для считывания строки с возможностью отмены
		bool PromptAndReadLine(string prompt, out string result)
		{
			Console.Write(prompt);
			if (!ReadLineWithEsc(out result))
			{
				cancelAction("Отмена удаления объекта из базы данных.");
				return false;
			}
			return true;
		}
		string ID;
		int id;
		DBManager.ExceptionManager transactionResult;

		while (!shouldExit)
		{
			switch (state)
			{
				case 0:
					selected_index = Menu(selected_index, ref state, menu_options, "", out shouldExit);
					break;
				case 1:
					switch (selected_index)
					{
						case 0:
							// Запросы на ввод данных
							if (!PromptAndReadLine("Введите идентификатор удаляемого ученика: ", out ID))
							{
								return;
							}

							if (!int.TryParse(ID, out id))
							{
								cancelAction("Неверный ввод!");
								return;
							}

							transactionResult = dbManager.DeleteStudent(id);
							if (transactionResult.code == ReturnCode.Success)
							{
								cancelAction("Ученик успешно удалён!");
								return;
							}
							else
							{
								if (transactionResult.code == ReturnCode.InternalError)
								{
									cancelAction("Что-то пошло не так.");

									return;
								}
								else
								{
									cancelAction("Ошибка в запросе.");
									return;
								}
							}
						case 1:
							// Запросы на ввод данных
							if (!PromptAndReadLine("Введите идентификатор удаляемого учителя: ", out ID))
							{
								return;
							}

							if (!int.TryParse(ID, out id))
							{
								cancelAction("Неверный ввод!");
								return;
							}

							transactionResult = dbManager.DeleteTeacher(id);
							if (transactionResult.code == ReturnCode.Success)
							{
								cancelAction("Учитель успешно удалён!");
								return;
							}
							else
							{
								if (transactionResult.code == ReturnCode.InternalError)
								{
									cancelAction("Что-то пошло не так.");

									return;
								}
								else
								{
									cancelAction("Ошибка в запросе.");
									return;
								}
							}
						case 2:
							if (!PromptAndReadLine("Введите идентификатор удаляемого курса: ", out ID))
							{
								return;
							}

							if (!int.TryParse(ID, out id))
							{
								cancelAction("Неверный ввод!");
								return;
							}

							transactionResult = dbManager.DeleteCourse(id);
							if (transactionResult.code == ReturnCode.Success)
							{
								cancelAction("Курс успешно удалён!");
								return;
							}
							else
							{
								if (transactionResult.code == ReturnCode.InternalError)
								{
									cancelAction("Что-то пошло не так.");

									return;
								}
								else
								{
									cancelAction("Ошибка в запросе.");
									return;
								}
							}
						case 3:
							if (!PromptAndReadLine("Введите идентификатор удаляемого экзамена: ", out ID))
							{
								return;
							}

							if (!int.TryParse(ID, out id))
							{
								cancelAction("Неверный ввод!");
								return;
							}

							transactionResult = dbManager.DeleteCourse(id);
							if (transactionResult.code == ReturnCode.Success)
							{
								cancelAction("Ученик успешно удалён!");
								return;
							}
							else
							{
								if (transactionResult.code == ReturnCode.InternalError)
								{
									cancelAction("Что-то пошло не так.");

									return;
								}
								else
								{
									cancelAction("Ошибка в запросе.");
									return;
								}
							}
						case 4:
							if (!PromptAndReadLine("Введите идентификатор удаляемой оценки: ", out ID))
							{
								return;
							}

							if (!int.TryParse(ID, out id))
							{
								cancelAction("Неверный ввод!");
								return;
							}

							transactionResult = dbManager.DeleteGrade(id);
							if (transactionResult.code == ReturnCode.Success)
							{
								cancelAction("Ученик успешно удалён!");
								return;
							}
							else
							{
								if (transactionResult.code == ReturnCode.InternalError)
								{
									cancelAction("Что-то пошло не так.");

									return;
								}
								else
								{
									cancelAction("Ошибка в запросе.");
									return;
								}
							}
					}
					return;
			}
		}

	}

	static void QueryStudentsPerFaculty(DBManager.Manager dbManager)
	{
		Console.Write("Введите название факультета: ");
		string s;
		ReadLineWithEsc(out s);
		var students = dbManager.StudentsByDepartment(s);

		if (students.Key.code == ReturnCode.Success && students.Value != null && students.Value.Count != 0)
		{
			Console.WriteLine($"Список студентов факультета {s}:");
			foreach (var student in students.Value)
			{
				Console.WriteLine($"\t{student}");
			}
		}
		else
		{
			Console.WriteLine("Студенты не найдены или произошла ошибка.");
		}
		cancelAction("");
	}

	static void QueryCoursesByInstructor(DBManager.Manager dbManager)
	{
		Console.Write("Введите имя и фамилию преподавателя: ");
		string s;
		ReadLineWithEsc(out s);

		string[] instructor = s.Split(" ");
		if (instructor.Length != 2)
		{
			cancelAction("Неверный формат ввода");
			return;
        }
		var courses = dbManager.CoursesByTeacher(instructor[0], instructor[1]);

		if (courses.Key.code == ReturnCode.Success && courses.Value != null && courses.Value.Count > 0)
		{
			Console.WriteLine($"Курсы, преподаваемые {String.Join(" ", instructor)}:");
			foreach (var course in courses.Value)
			{
				Console.WriteLine($"\t{course}");
			}
		}
		else
		{
			Console.WriteLine("Курсы не найдены или произошла ошибка.");
		}
		cancelAction("");
	}

	static void MassExecute(DBManager.Manager dbManager)
	{
		StringBuilder input = new StringBuilder();
		Console.WriteLine("Введите команды SQL. Закончите ввод комбинацией клавиш CTRL+Enter.");

		while (true)
		{
			ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

			// Check if Ctrl + Enter is pressed
			if (keyInfo.Key == ConsoleKey.Enter && (keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
			{
				Console.WriteLine(); // Move to the next line
				var massExecuteResult = dbManager.MassExecute(new List<string>(input.ToString().Split("\n")));
				Console.Clear();

				Console.ReadKey();
				if (massExecuteResult != null)
				{
					if (massExecuteResult.code == ReturnCode.Success)
					{
						cancelAction("Успешно выполнены команды!");
						return;
					}
					else
					{
						if (massExecuteResult.code == ReturnCode.InternalError)
						{
							cancelAction("Что-то пошло не так!");
							return;
						}
						else
						{
							cancelAction("Неверный ввод.");
							return;
						}
					}
				}
				else
				{
					cancelAction("Что-то пошло не так!");
					return;
				}
			}

			if (keyInfo.Key == ConsoleKey.Enter)
			{
				Console.WriteLine();
				input.AppendLine();
			}
			else if (keyInfo.Key == ConsoleKey.Backspace)
			{
				if (input.Length > 0)
				{
					input.Remove(input.Length - 1, 1);

					Console.Write("\b \b");
				}
			}
			else
			{
				Console.Write(keyInfo.KeyChar);
				input.Append(keyInfo.KeyChar);
			}
		}
	}

	static void QueryStudentsByCourse(DBManager.Manager dbManager)
	{
		Console.Write("Введите название курса: ");
		string s;
		ReadLineWithEsc(out s);

		var courses = dbManager.StudentsByCourse(s);

		if (courses.Key.code == ReturnCode.Success && courses.Value != null && courses.Value.Count > 0)
		{
			Console.WriteLine($"Ученики на курсе {s}:");
			foreach (var course in courses.Value)
			{
				Console.WriteLine($"\t{String.Join(" ", course)}");
			}
		}
		else
		{
			Console.WriteLine("Курсы не найдены или произошла ошибка.");
		}
		cancelAction("");
	}

	static void QueryGradesByCourse(DBManager.Manager dbManager)
	{
		Console.Write("Введите название курса: ");
		string s;
		ReadLineWithEsc(out s);

		var courses = dbManager.GradesByCourse(s);

		if (courses.Key.code == ReturnCode.Success && courses.Value != null && courses.Value.Count > 0)
		{
			Console.WriteLine($"Оценки на курсе {s}:");
			foreach (var course in courses.Value)
			{
				Console.WriteLine($"\t{String.Join(" ", course)}");
			}
		}
		else
		{
			Console.WriteLine("Курсы не найдены или произошла ошибка.");
		}
		cancelAction("");
	}

	static void CalculateAverageGradeByCourse(DBManager.Manager dbManager)
	{
		Console.Write("Введите название курса: ");
		string s;
		ReadLineWithEsc(out s);

		var courses = dbManager.AverageByCourse(s);

		if (courses.Key.code == ReturnCode.Success && courses.Value != null)
		{
			Console.WriteLine($"Средняя оценка на курсе {s}: {courses.Value}");
		}
		else
		{
			Console.WriteLine("Оценки не найдены или произошла ошибка.");
		}
		cancelAction("");
	}

	static void CalculateOverallAverageGrade(DBManager.Manager dbManager)
	{
		Console.Write("Введите имя и фамилию студента: ");
		string s;
		ReadLineWithEsc(out s);
		string[] ss = s.Split(" ");

		if (ss.Length != 2)
		{
			cancelAction("Неверный формат ввода!");
			return;
		}

		var courses = dbManager.AverageStudentMark(ss[0], ss[1]);

		if (courses.Key.code == ReturnCode.Success && courses.Value != null)
		{
			Console.WriteLine($"Средняя оценка на курсе {s}: {courses.Value}");
		}
		else
		{
			Console.WriteLine("Оценки не найдены или произошла ошибка.");
		}
		cancelAction("");
	}

	static void CalculateAverageGradeByFaculty(DBManager.Manager dbManager)
	{
		Console.Write("Введите название факультета: ");
		string s;
		ReadLineWithEsc(out s);

		var courses = dbManager.AverageOnDepartment(s);

		if (courses.Key.code == ReturnCode.Success && courses.Value != null)
		{
			Console.WriteLine($"Средняя оценка на факультете {s}: {courses.Value}");
		}
		else
		{
			Console.WriteLine("Оценки не найдены или произошла ошибка.");
		}
		cancelAction("");
	}

	static void DisplayTable(DBManager.Manager dbManager)
	{
		string[] options =
		{
			"1. Студенты",
			"2. Учителя",
			"3. Курсы",
			"4. Экзамены",
			"5. Оценки"
		};

		bool shouldExit = false;
		int selectedIndex = 0;
		bool[] b = { false, false, false, false, false };
		int state = 0;
		while (!shouldExit && state == 0)
			selectedIndex = MenuSelector(selectedIndex, ref state, options, "Какия таблицы вывести:", out shouldExit, ref b);

		if (shouldExit)
		{
			cancelAction("");
			return;
		}
		if (b[0])
			Console.WriteLine(String.Join("\n", dbManager.GetTable("Students").Value));
		if (b[1])
			Console.WriteLine(String.Join("\n", dbManager.GetTable("Teachers").Value));
		if (b[2])
			Console.WriteLine(String.Join("\n", dbManager.GetTable("Courses").Value));
		if (b[3])
			Console.WriteLine(String.Join("\n", dbManager.GetTable("Exams").Value));
		if (b[4])
			Console.WriteLine(String.Join("\n", dbManager.GetTable("Grades").Value));

		cancelAction("");
	}

	// Остальные функции аналогично структуре, приведённой выше
}
