using System;
using MySql.Data.MySqlClient;

namespace university_school_task
{
	class Program
	{
		static int Menu(int selectedIndex, ref int state, string[] options, string starting_line)
		{
			Console.Clear();
			Console.WriteLine(starting_line);

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
			return selectedIndex;
		}

		static void Main()
		{
			string connectionString = "Server=localhost;User ID=root;Password=LYRzwBwouSIv5oAvwQeF;";

			using (var connection = new MySqlConnection(connectionString))
			{
				connection.Open();

				// Check and create the database if it doesn't exist
				CreateDatabaseIfNotExists(connection, "university");

				var dbManager = new DBManager.Manager(connection);
				bool online = true;
				int selectedIndex = 0;
				int state = 0;

				string[] menuGeneral =
				{
					" 1. Добавление нового студента, преподавателя, курса, экзамена и оценки.",
					" 2. Изменение информации о студентах, преподавателях и курсах",
					" 3. Удаление студентов, преподавателей, курсов и экзаменов.",
					" 4. Получение списка студентов по факультету.",
					" 5. Получение списка курсов, читаемых определенным преподавателем",
					" 6. Получение списка студентов, зачисленных на конкретный курс",
					" 7. Получение оценок студентов по определенному курсу.",
					" 8. Средний балл студента по определенному курсу.",
					" 9. Средний балл студента в целом.",
					"10. Средний балл по факультету",
					"11. Выход"
				};
				string[] menuChange =
				{
					"1. Студента",
					"2. Преподавателя",
					"3. Курс",
					"4. Экзамен",
					"5. Оценку"
				};
				string[] menuCurrent = menuGeneral;

				string textValue = "";
				string query = "";
				string answer = "";

				selectedIndex = Menu(selectedIndex, ref state, menuCurrent, "Меню:");
				while (online)
				{
					if (state == -1)
					{
						Console.Clear();
						Console.WriteLine(textValue);
						var key = Console.ReadKey(true);

						state = 0;
					}
					if (state == 0 || state == 1)
						selectedIndex = Menu(selectedIndex, ref state, menuCurrent, "Меню:");
					if (state == 1)
					{
						switch (selectedIndex)
						{
							case 0 or 1 or 2:
								menuCurrent = menuChange;
								break;
							case 3:
								state = 2;
								query = " 4. Получение списка студентов по факультету.\n\tФакультет: ";
								break;
							case 4:
								break;
							case 5:
								break;
							case 6:
								break;
							case 7:
								break;
							case 8:
								break;
							case 9:
								break;
							case 10:
								online = false;
								state = -1;
								break;
						}
					}
					if (state == 2)
					{
						Console.Write(query);
						answer = Console.ReadLine() ?? "";
					}
				}
				// Что-то важное сделать
				// Например закрыть БД или хз что
				connection.Close();
				return;
			}
		}

		static void CreateDatabaseIfNotExists(MySqlConnection connection, string dbName)
		{
			var commandText = $"CREATE DATABASE IF NOT EXISTS `{dbName}`;";

			using (var command = new MySqlCommand(commandText, connection))
			{
				command.ExecuteNonQuery(); // Execute the command
			}
		}
	}
}
