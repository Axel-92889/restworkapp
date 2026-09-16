using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace RestaurantWorkApp
{
    public partial class PromotionsControl : UserControl
    {
        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";

        public PromotionsControl()
        {
            InitializeComponent();
            Loaded += PromotionsControl_Loaded; // Проверка прав доступа при загрузке
        }

        private void PromotionsControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();

            if (!AccessControl.CanDoOperation("Promotions"))
            {
                btnAdd.IsEnabled = false;
                btnDelete.IsEnabled = false;
                dgLigota.IsReadOnly = true;
                dgLigota.CanUserAddRows = false;
                dgLigota.CanUserDeleteRows = false;
                txtLigotaName.IsEnabled = false;
                txtLigotaName.ToolTip = "У вас нет прав на редактирование";
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Promotions"))
            {
                MessageBox.Show("У вас нет прав для добавления акции!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string ligotaName = txtLigotaName.Text.Trim();

            if (string.IsNullOrEmpty(ligotaName))
            {
                MessageBox.Show("Введите название акции");
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    string query = "INSERT INTO Promotions (Name_P) VALUES (@name)";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@name", ligotaName);

                    connection.Open();
                    command.ExecuteNonQuery();

                    txtLigotaName.Clear();
                    LoadData();
                    MessageBox.Show("Акция добавлена успешно!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении акции: {ex.Message}");
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void dgLigota_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Promotions"))
            {
                MessageBox.Show("У вас нет прав для редактирования акции!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataGrid grid = sender as DataGrid;
                if (grid != null)
                {
                    DataRowView rowView = e.Row.Item as DataRowView;
                    if (rowView != null)
                    {
                        if (e.Column != null && e.Column.Header.ToString() == "Название акции")
                        {
                            string newLigotaName = null;

                            if (e.EditingElement is TextBox textBox)
                            {
                                newLigotaName = textBox.Text.Trim();
                            }

                            if (string.IsNullOrEmpty(newLigotaName))
                            {
                                MessageBox.Show("Название акции не может быть пустым");
                                e.Cancel = true;
                                return;
                            }

                            int ligotaId = Convert.ToInt32(rowView["id_Promotion"]); // Обратите внимание на правильное имя столбца

                            UpdatePromotionInDatabase(ligotaId, newLigotaName);
                        }
                    }
                }
            }
        }

        // Обновление акции в БД
        private void UpdatePromotionInDatabase(int promotionId, string newPromotionName)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    string query = "UPDATE Promotions SET Name_P = @name WHERE id_Promotion = @id"; // Исправлено имя таблицы и столбцов
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@name", newPromotionName);
                    command.Parameters.AddWithValue("@id", promotionId);

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        //MessageBox.Show("Акция обновлена успешно!"); // Можно не показывать
                    }
                    else
                    {
                        MessageBox.Show("Акция не была обновлена.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении акции: {ex.Message}");
                LoadData();
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Promotions"))
            {
                MessageBox.Show("У вас нет прав для удаления акции!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgLigota.SelectedItem == null)
            {
                MessageBox.Show("Выберите акцию для удаления");
                return;
            }

            DataRowView selectedRow = dgLigota.SelectedItem as DataRowView;
            int id_promotion = Convert.ToInt32(selectedRow["id_Promotion"]); // Исправлено имя столбца
            string name = selectedRow["Name_P"].ToString(); // Исправлено имя столбца

            var result = MessageBox.Show($"Вы уверены, что хотите удалить акцию '{name}'? ",
                                        "Подтверждение удаления ",
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
                                string deleteQuery = "DELETE FROM Promotions WHERE id_Promotion = @id"; // Исправлено имя таблицы и столбца
                                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                                deleteCmd.Parameters.AddWithValue("@id", id_promotion);
                                int rowsAffected = deleteCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // 2. Сброс AUTO_INCREMENT
                                    string getMaxIdQuery = "SELECT MAX(id_Promotion) as max_id FROM Promotions"; // Исправлено имя столбца
                                    MySqlCommand getMaxIdCmd = new MySqlCommand(getMaxIdQuery, conn, transaction);
                                    object maxIdResult = getMaxIdCmd.ExecuteScalar();

                                    int newAutoIncrementValue = 1;
                                    if (maxIdResult != null && maxIdResult != DBNull.Value)
                                    {
                                        newAutoIncrementValue = Convert.ToInt32(maxIdResult) + 1;
                                    }

                                    string alterQuery = "ALTER TABLE Promotions AUTO_INCREMENT = @new_auto_increment_value"; // Исправлено имя таблицы
                                    MySqlCommand alterCmd = new MySqlCommand(alterQuery, conn, transaction);
                                    alterCmd.Parameters.AddWithValue("@new_auto_increment_value", newAutoIncrementValue);
                                    alterCmd.ExecuteNonQuery();

                                    transaction.Commit();
                                    LoadData();
                                    MessageBox.Show("Акция удалена успешно!");
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Ошибка: Акция не найдена или не была удалена.");
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
                    MessageBox.Show($"Ошибка при удалении акции: {ex.Message}");
                }
            }
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Promotions ORDER BY id_Promotion"; // Исправлено имя столбца
                    MySqlCommand command = new MySqlCommand(query, connection);

                    connection.Open();

                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    dgLigota.ItemsSource = dataTable.DefaultView; // Исправлено имя DataGrid
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }
    }
}