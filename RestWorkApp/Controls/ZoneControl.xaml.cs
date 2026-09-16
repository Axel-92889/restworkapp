using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Media; // Для Brushes

namespace RestaurantWorkApp.Controls
{
    public partial class ZoneControl : UserControl
    {
        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";

        public ZoneControl()
        {
            InitializeComponent();
            Loaded += ZoneControl_Loaded; // Проверка прав доступа при загрузке
        }

        private void ZoneControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();

            if (!AccessControl.CanDoOperation("Zone")) // Предполагается, что в AccessControl есть проверка для "Zone"
            {
                btnAdd.IsEnabled = false;
                btnDelete.IsEnabled = false;
                dgZones.IsReadOnly = true;
                dgZones.CanUserAddRows = false;
                dgZones.CanUserDeleteRows = false;
                txtZoneName.IsEnabled = false;
                txtZoneName.ToolTip = "У вас нет прав на редактирование";
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Zone"))
            {
                MessageBox.Show("У вас нет прав для добавления зоны!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string zoneName = txtZoneName.Text.Trim();

            if (string.IsNullOrEmpty(zoneName))
            {
                MessageBox.Show("Введите название зоны");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = "INSERT INTO Zone (Name_Zone) VALUES (@name)";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@name", zoneName);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    txtZoneName.Clear();
                    LoadData();
                    MessageBox.Show("Зона добавлена успешно!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении зоны: {ex.Message}");
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Zone"))
            {
                MessageBox.Show("У вас нет прав для удаления зоны!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgZones.SelectedItem == null)
            {
                MessageBox.Show("Выберите зону для удаления");
                return;
            }

            DataRowView selectedRow = dgZones.SelectedItem as DataRowView;
            int id = Convert.ToInt32(selectedRow["id_Zone"]);
            string name = selectedRow["Name_Zone"].ToString();

            var result = MessageBox.Show($"Удалить зону '{name}'?",
                                         "Подтверждение удаления",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        {
                            try
                            {
                                // 1. Удаление записи
                                string deleteQuery = "DELETE FROM Zone WHERE id_Zone = @id";
                                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                                deleteCmd.Parameters.AddWithValue("@id", id);
                                int rowsAffected = deleteCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // 2. Сброс AUTO_INCREMENT
                                    string getMaxIdQuery = "SELECT MAX(id_Zone) as max_id FROM Zone";
                                    MySqlCommand getMaxIdCmd = new MySqlCommand(getMaxIdQuery, conn, transaction);
                                    object maxIdResult = getMaxIdCmd.ExecuteScalar();

                                    int newAutoIncrementValue = 1;
                                    if (maxIdResult != null && maxIdResult != DBNull.Value)
                                    {
                                        newAutoIncrementValue = Convert.ToInt32(maxIdResult) + 1;
                                    }

                                    string alterQuery = "ALTER TABLE Zone AUTO_INCREMENT = @new_auto_increment_value";
                                    MySqlCommand alterCmd = new MySqlCommand(alterQuery, conn, transaction);
                                    alterCmd.Parameters.AddWithValue("@new_auto_increment_value", newAutoIncrementValue);
                                    alterCmd.ExecuteNonQuery();

                                    transaction.Commit();
                                    LoadData();
                                    MessageBox.Show("Зона удалена успешно!");
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Ошибка: Зона не найдена или не была удалена.");
                                }
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении зоны: {ex.Message}");
                }
            }
        }

        private void dgZones_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Zone"))
            {
                MessageBox.Show("У вас нет прав для редактирования зоны!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataRowView rowView = e.Row.Item as DataRowView;
                if (rowView == null) return;

                int id = Convert.ToInt32(rowView["id_Zone"]);
                string newName = (e.EditingElement as TextBox)?.Text.Trim();

                if (string.IsNullOrEmpty(newName))
                {
                    MessageBox.Show("Название зоны не может быть пустым");
                    e.Cancel = true;
                    return;
                }

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        string query = "UPDATE Zone SET Name_Zone = @name WHERE id_Zone = @id";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@name", newName);
                        cmd.Parameters.AddWithValue("@id", id);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении: {ex.Message}");
                    LoadData();
                }
            }
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Zone ORDER BY id_Zone";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgZones.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }
    }
}