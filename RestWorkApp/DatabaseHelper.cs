using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace RestaurantWorkApp
{
    internal class DatabaseHelper
    {
        private static string connectionString = "server=localhost;port=3306;username=root;password=;database=restaurant_db";

        public static DataTable ExecuteQuery(string query, params MySqlParameter[] parameters)
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    connection.Open();
                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    adapter.Fill(dataTable);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка выполнения запроса: {ex.Message}", ex);
            }

            return dataTable;
        }

        public static int ExecuteNonQuery(string query, params MySqlParameter[] parameters)
        {
            int rowsAffected = 0;

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    connection.Open();
                    rowsAffected = command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка выполнения запроса: {ex.Message}", ex);
            }

            return rowsAffected;
        }

        // Методы для конкретных таблиц
        public static DataTable GetRules() => ExecuteQuery("SELECT * FROM Rule ORDER BY id_Rule");
        public static DataTable GetLigota() => ExecuteQuery("SELECT * FROM Ligota ORDER BY id_Ligota");
        public static DataTable GetMarshrut() => ExecuteQuery("SELECT * FROM Marshrut ORDER BY id_Marshrut");
        public static DataTable GetBus() => ExecuteQuery("SELECT * FROM Bus ORDER BY id_Bus");
        public static DataTable GetStops() => ExecuteQuery("SELECT * FROM Stop ORDER BY id_Stop");
        public static DataTable GetPassengers() => ExecuteQuery("SELECT * FROM Passenger ORDER BY id_Passenger");
        public static DataTable GetUsers() => ExecuteQuery("SELECT u.*, r.name_role FROM User u LEFT JOIN UserRole r ON u.id_role = r.id_role ORDER BY u.id_User");
    }
}