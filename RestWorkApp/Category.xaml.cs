using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp3
{
    /// <summary>
    /// Логика взаимодействия для Category.xaml
    /// </summary>
    public partial class Category : UserControl
    {
        public Category()
        {
            InitializeComponent();
            LoadData();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string Name_R = NazRolTextBox.Text.Trim();

            if (string.IsNullOrEmpty(Name_R))
            {
                MessageBox.Show("Введите название роли");
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    string query = "INSERT INTO Rule (Name_R) VALUES (@name)";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@name", Name_R);

                    connection.Open();
                    command.ExecuteNonQuery();

                    NazRolTextBox.Clear();
                    LoadData();
                    MessageBox.Show("Роль добавлена успешно!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении роли: {ex.Message}");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void RolesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataGrid grid = sender as DataGrid;
                if (grid != null)
                {
                    // Получаем отредактированную строку
                    DataRowView rowView = e.Row.Item as DataRowView;
                    if (rowView != null)
                    {
                        // Проверяем, что редактируется столбец Name_R (не id_Rule)
                        if (e.Column != null && e.Column.Header.ToString() == "Name_R")
                        {
                            string newRoleName = null;

                            // Получаем новое значение из TextBox
                            if (e.EditingElement is TextBox textBox)
                            {
                                newRoleName = textBox.Text.Trim();
                            }

                            if (string.IsNullOrEmpty(newRoleName))
                            {
                                MessageBox.Show("Название роли не может быть пустым");
                                e.Cancel = true; // Отменяем редактирование
                                return;
                            }

                            int roleId = Convert.ToInt32(rowView["id_Rule"]);

                            // Сохраняем изменения в базе данных
                            UpdateRoleInDatabase(roleId, newRoleName);
                        }
                    }
                }
            }
        }

        // Метод для обновления роли в базе данных
        private void UpdateRoleInDatabase(int roleId, string newRoleName)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    string query = "UPDATE Rule SET Name_R = @name WHERE id_Rule = @id";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@name", newRoleName);
                    command.Parameters.AddWithValue("@id", roleId);

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Название роли обновлено успешно!");
                    }
                    else
                    {
                        MessageBox.Show("Роль не найдена");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении роли: {ex.Message}");
                // При ошибке перезагружаем данные из базы
                LoadData();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RolesDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите роль для удаления");
                return;
            }

            DataRowView selectedRow = RolesDataGrid.SelectedItem as DataRowView;
            int id_Rule = Convert.ToInt32(selectedRow["id_Rule"]);

            var result = MessageBox.Show($"Вы уверены, что хотите удалить роль '{selectedRow["Name_R"]}'?",
                                       "Подтверждение удаления",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        string query = "DELETE FROM Rule WHERE id_Rule = @id";
                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@id", id_Rule);

                        connection.Open();
                        command.ExecuteNonQuery();

                        LoadData();
                        MessageBox.Show("Роль удалена успешно!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении роли: {ex.Message}");
                }
            }
        }

        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db;";

        private void LoadData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Rule"; // Замените на вашу таблицу
                    MySqlCommand command = new MySqlCommand(query, connection);

                    connection.Open();

                    // Используем MySqlDataAdapter для заполнения DataTable
                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    // Устанавливаем DataTable как источник данных для DataGrid
                    RolesDataGrid.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }
    }
}
