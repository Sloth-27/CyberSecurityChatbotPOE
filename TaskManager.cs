using System;
using System.Collections.Generic;
using MySqlConnector;

namespace CyberSecurityChatbotGUI
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime? ReminderDate { get; set; }
        public bool IsCompleted { get; set; }
    }

    // Handles all database work for the task assistant
    public class TaskManager
    {
        private const string ConnectionString =
            "Server=localhost;Port=3306;Database=chatbot_db;User=root;Password=Chatbot123;";

        public int AddTask(string title, string description, DateTime? reminder)
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new MySqlCommand(
                "INSERT INTO tasks (title, description, reminder_date) VALUES (@t, @d, @r); SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@t", title);
            cmd.Parameters.AddWithValue("@d", description);
            cmd.Parameters.AddWithValue("@r", (object?)reminder ?? DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<TaskItem> GetTasks()
        {
            var list = new List<TaskItem>();
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new MySqlCommand("SELECT id, title, description, reminder_date, is_completed FROM tasks ORDER BY id", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new TaskItem
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    ReminderDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                    IsCompleted = reader.GetBoolean(4)
                });
            }
            return list;
        }

        public void SetReminder(int id, DateTime date)
        {
            Execute("UPDATE tasks SET reminder_date = @r WHERE id = @id", ("@r", date), ("@id", id));
        }

        public bool Complete(int id)
        {
            return Execute("UPDATE tasks SET is_completed = TRUE WHERE id = @id", ("@id", id));
        }

        public bool Delete(int id)
        {
            return Execute("DELETE FROM tasks WHERE id = @id", ("@id", id));
        }

        // Runs one command and returns true if a row was changed
        private bool Execute(string sql, params (string name, object value)[] parameters)
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            foreach (var p in parameters)
                cmd.Parameters.AddWithValue(p.name, p.value);
            return cmd.ExecuteNonQuery() > 0;
        }
    }
}