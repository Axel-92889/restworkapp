using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace RestaurantWorkApp.Controls
{
    public partial class FeedbackControl : UserControl
    {
        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";

        public FeedbackControl()
        {
            InitializeComponent();
            Loaded += FeedbackControl_Loaded; // Проверка прав доступа при загрузке
        }

        private void FeedbackControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();

            if (!AccessControl.CanDoOperation("Feedback"))
            {
                btnAdd.IsEnabled = false;
                btnDelete.IsEnabled = false;
                dgFeedback.IsReadOnly = true;
                dgFeedback.CanUserAddRows = false;
                dgFeedback.CanUserDeleteRows = false;
                txtRating.IsEnabled = false;
                txtComment.IsEnabled = false;
                txtClientId.IsEnabled = false;
                txtZakazId.IsEnabled = false;
            }
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    // Сортировка по дате (новые сверху)
                    string query = "SELECT * FROM Feedback ORDER BY Date_Feedback DESC ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgFeedback.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Feedback"))
            {
                MessageBox.Show("У вас нет прав для добавления отзыва!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string ratingStr = txtRating.Text.Trim();
            string comment = txtComment.Text.Trim();
            string clientIdStr = txtClientId.Text.Trim();
            string zakazIdStr = txtZakazId.Text.Trim();

            // 1. Валидация Рейтинг (1-5)
            if (!int.TryParse(ratingStr, out int rating) || rating < 1 || rating > 5)
            {
                MessageBox.Show("Рейтинг должен быть целым числом от 1 до 5 ");
                return;
            }

            // 2. Валидация ID Клиента (обязательно)
            if (!int.TryParse(clientIdStr, out int clientId))
            {
                MessageBox.Show("Введите корректный ID клиента ");
                return;
            }

            // 3. ID Заказа (необязательно, nullable)
            int? zakazId = null;
            if (!string.IsNullOrEmpty(zakazIdStr))
            {
                if (!int.TryParse(zakazIdStr, out int zId))
                {
                    MessageBox.Show("Неверный формат ID заказа ");
                    return;
                }
                zakazId = zId;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    // Date_Feedback имеет значение по умолчанию CURRENT_TIMESTAMP,
                    // поэтому мы можем его не передавать, если хотим  "сейчас ".
                    string query = "INSERT INTO Feedback (Rating, Comment, id_client, id_zakaz) VALUES (@rating, @comment, @client, @zakaz)";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@rating", rating);
                    cmd.Parameters.AddWithValue("@comment", string.IsNullOrEmpty(comment) ? (object)DBNull.Value : comment);
                    cmd.Parameters.AddWithValue("@client", clientId);
                    cmd.Parameters.AddWithValue("@zakaz", zakazId.HasValue ? zakazId.Value : (object)DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    // Очистка
                    txtRating.Clear();
                    txtComment.Clear();
                    txtClientId.Clear();
                    txtZakazId.Clear();

                    LoadData();
                    MessageBox.Show("Отзыв добавлен успешно! ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message} ");
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Feedback"))
            {
                MessageBox.Show("У вас нет прав для удаления отзыва!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgFeedback.SelectedItem == null)
            {
                MessageBox.Show("Выберите отзыв для удаления ");
                return;
            }

            DataRowView selectedRow = dgFeedback.SelectedItem as DataRowView;
            int id = Convert.ToInt32(selectedRow["id_Feedback"]);

            var result = MessageBox.Show($"Удалить отзыв ID: {id}? ",
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
                                string deleteQuery = "DELETE FROM Feedback WHERE id_Feedback = @id";
                                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                                deleteCmd.Parameters.AddWithValue("@id", id);
                                int rowsAffected = deleteCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // 2. Сброс AUTO_INCREMENT
                                    string getMaxIdQuery = "SELECT MAX(id_Feedback) as max_id FROM Feedback";
                                    MySqlCommand getMaxIdCmd = new MySqlCommand(getMaxIdQuery, conn, transaction);
                                    object maxIdResult = getMaxIdCmd.ExecuteScalar();

                                    int newAutoIncrementValue = 1;
                                    if (maxIdResult != null && maxIdResult != DBNull.Value)
                                    {
                                        newAutoIncrementValue = Convert.ToInt32(maxIdResult) + 1;
                                    }

                                    string alterQuery = "ALTER TABLE Feedback AUTO_INCREMENT = @new_auto_increment_value";
                                    MySqlCommand alterCmd = new MySqlCommand(alterQuery, conn, transaction);
                                    alterCmd.Parameters.AddWithValue("@new_auto_increment_value", newAutoIncrementValue);
                                    alterCmd.ExecuteNonQuery();

                                    transaction.Commit();
                                    LoadData();
                                    MessageBox.Show("Отзыв удалён успешно! ");
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Ошибка: Отзыв не найден или не был удалён.");
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
                    MessageBox.Show($"Ошибка при удалении: {ex.Message} ");
                }
            }
        }

        private void dgFeedback_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Feedback"))
            {
                MessageBox.Show("У вас нет прав для редактирования отзыва!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataRowView rowView = e.Row.Item as DataRowView;
                if (rowView == null) return;

                int id = Convert.ToInt32(rowView["id_Feedback"]);
                string header = e.Column.Header as string;
                string newValue = (e.EditingElement as TextBox)?.Text.Trim();

                if (string.IsNullOrEmpty(header)) return;

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        string query = " ";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", id);

                        switch (header)
                        {
                            case "Рейтинг":
                                if (!int.TryParse(newValue, out int rating) || rating < 1 || rating > 5)
                                {
                                    MessageBox.Show("Рейтинг должен быть от 1 до 5 ");
                                    e.Cancel = true;
                                    return;
                                }
                                query = "UPDATE Feedback SET Rating = @val WHERE id_Feedback = @id";
                                cmd.Parameters.AddWithValue("@val", rating);
                                break;

                            case "Комментарий":
                                query = "UPDATE Feedback SET Comment = @val WHERE id_Feedback = @id";
                                cmd.Parameters.AddWithValue("@val", string.IsNullOrEmpty(newValue) ? (object)DBNull.Value : newValue);
                                break;

                            case "ID Клиента":
                                if (!int.TryParse(newValue, out int clientId))
                                {
                                    MessageBox.Show("Неверный формат ID клиента ");
                                    e.Cancel = true;
                                    return;
                                }
                                query = "UPDATE Feedback SET id_client = @val WHERE id_Feedback = @id";
                                cmd.Parameters.AddWithValue("@val", clientId);
                                break;

                            case "ID Заказа":
                                if (string.IsNullOrEmpty(newValue))
                                    query = "UPDATE Feedback SET id_zakaz = NULL WHERE id_Feedback = @id";
                                else if (int.TryParse(newValue, out int zakazId))
                                {
                                    query = "UPDATE Feedback SET id_zakaz = @val WHERE id_Feedback = @id";
                                    cmd.Parameters.AddWithValue("@val", zakazId);
                                }
                                else
                                {
                                    MessageBox.Show("Неверный формат ID заказа ");
                                    e.Cancel = true;
                                    return;
                                }
                                break;

                            default:
                                return; // ID и Дату не редактируем в этом примере
                        }

                        if (!string.IsNullOrEmpty(query))
                        {
                            cmd.CommandText = query;
                            conn.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении: {ex.Message} ");
                    LoadData();
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
    }
}