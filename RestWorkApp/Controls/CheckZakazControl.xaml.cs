using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Для Brushes

namespace RestaurantWorkApp.Controls
{
    public partial class CheckZakazControl : UserControl
    {
        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";

        public CheckZakazControl()
        {
            InitializeComponent();
            Loaded += CheckZakazControl_Loaded; // Проверка прав доступа при загрузке
        }

        private void CheckZakazControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();

            if (!AccessControl.CanDoOperation("Checkzakaz")) // Предполагается, что в AccessControl есть проверка для "Checkzakaz"
            {
                btnAdd.IsEnabled = false;
                btnDelete.IsEnabled = false;
                dgChecks.IsReadOnly = true;
                dgChecks.CanUserAddRows = false;
                dgChecks.CanUserDeleteRows = false;
                dpTimeCheckDate.IsEnabled = false;
                txtTimeCheckTime.IsEnabled = false;
                txtDishesInfo.IsEnabled = false;
                txtTotalAmount.IsEnabled = false;
                txtPayment.IsEnabled = false;
                txtIdZakaz.IsEnabled = false;
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Checkzakaz"))
            {
                MessageBox.Show("У вас нет прав для добавления чека!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Соби раем данные из полей
            string timeCheckStr = $"{dpTimeCheckDate.Text} {txtTimeCheckTime.Text} ".Trim();
            string dishesInfo = txtDishesInfo.Text.Trim();
            string totalAmountStr = txtTotalAmount.Text.Trim();
            string payment = txtPayment.Text.Trim();
            string idZakazStr = txtIdZakaz.Text.Trim();

            // Валидация
            if (string.IsNullOrEmpty(timeCheckStr) || dpTimeCheckDate.SelectedDate == null)
            {
                MessageBox.Show("Введите корректную дату и время чека ");
                return;
            }
            if (string.IsNullOrEmpty(dishesInfo))
            {
                MessageBox.Show("Введите информацию о блюдах ");
                return;
            }
            if (string.IsNullOrEmpty(totalAmountStr) || !decimal.TryParse(totalAmountStr, out decimal totalAmount))
            {
                MessageBox.Show( "Введите корректную сумму (число) ");
                return;
            }
            if (string.IsNullOrEmpty(payment))
            {
                MessageBox.Show("Введите способ оплаты ");
                return;
            }
            if (string.IsNullOrEmpty(idZakazStr) || !int.TryParse(idZakazStr, out int idZakaz))
            {
                MessageBox.Show("Введите корректный ID заказа (целое число) ");
                return;
            }

            DateTime timeCheck;
            if (!DateTime.TryParse(timeCheckStr, out timeCheck))
            {
                MessageBox.Show("Не удалось распознать дату и время. Используйте формат ДД.ММ.ГГГГ чч:мм ");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = @"INSERT INTO CheckZakaz (Time_Check, Dishes_Info, Total_Amount, Payment, id_zakaz) 
                                     VALUES(@time, @dishes, @total, @payment, @idZakaz) ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@time ", timeCheck);
                    cmd.Parameters.AddWithValue("@dishes ", dishesInfo);
                    cmd.Parameters.AddWithValue("@total ", totalAmount);
                    cmd.Parameters.AddWithValue("@payment ", payment);
                    cmd.Parameters.AddWithValue("@idZakaz ", idZakaz);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    // Очистка полей
                    dpTimeCheckDate.Text = " ";
                    txtTimeCheckTime.Text = "00:00 ";
                    txtDishesInfo.Clear();
                    txtTotalAmount.Clear();
                    txtPayment.Clear();
                    txtIdZakaz.Clear();

                    LoadData();
                    MessageBox.Show("Чек добавлен успешно! ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении чека: {ex.Message} ");
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Checkzakaz"))
            {
                MessageBox.Show("У вас нет прав для удаления чека!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgChecks.SelectedItem == null)
            {
                MessageBox.Show("Выберите чек для удаления ");
                return;
            }

            DataRowView selectedRow = dgChecks.SelectedItem as DataRowView;
            int id = Convert.ToInt32(selectedRow["id_Check "]);
            string info = selectedRow["Dishes_Info "].ToString();

            var result = MessageBox.Show($"Удалить чек №{id} ('{info}')? ",
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
                                string deleteQuery = "DELETE FROM CheckZakaz WHERE id_Check = @id ";
                                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                                deleteCmd.Parameters.AddWithValue("@id ", id);
                                int rowsAffected = deleteCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // 2. Сброс AUTO_INCREMENT
                                    string getMaxIdQuery = "SELECT MAX(id_Check) as max_id FROM CheckZakaz";
                                    MySqlCommand getMaxIdCmd = new MySqlCommand(getMaxIdQuery, conn, transaction);
                                    object maxIdResult = getMaxIdCmd.ExecuteScalar();

                                    int newAutoIncrementValue = 1;
                                    if (maxIdResult != null && maxIdResult != DBNull.Value)
                                    {
                                        newAutoIncrementValue = Convert.ToInt32(maxIdResult) + 1;
                                    }

                                    string alterQuery = "ALTER TABLE CheckZakaz AUTO_INCREMENT = @new_auto_increment_value";
                                    MySqlCommand alterCmd = new MySqlCommand(alterQuery, conn, transaction);
                                    alterCmd.Parameters.AddWithValue("@new_auto_increment_value", newAutoIncrementValue);
                                    alterCmd.ExecuteNonQuery();

                                    transaction.Commit();
                                    LoadData();
                                    MessageBox.Show("Чек удалён успешно! ");
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Ошибка: Чек не найден или не был удалён.");
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
                    MessageBox.Show($"Ошибка при удалении чека: {ex.Message} ");
                }
            }
        }

        private void dgChecks_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Checkzakaz"))
            {
                MessageBox.Show("У вас нет прав для редактирования чека!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataRowView rowView = e.Row.Item as DataRowView;
                if (rowView == null) return;

                // Разрешаем редактировать только столбец  "Оплата " (Payment)
                if (e.Column is DataGridTextColumn column && column.Header.ToString() == "Оплата ")
                {
                    int id = Convert.ToInt32(rowView["id_Check "]);
                    string newPayment = (e.EditingElement as TextBox)?.Text.Trim();

                    if (string.IsNullOrEmpty(newPayment))
                    {
                        MessageBox.Show("Способ оплаты не может быть пустым ");
                        e.Cancel = true;
                        return;
                    }

                    try
                    {
                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            string query = "UPDATE CheckZakaz SET Payment = @payment WHERE id_Check = @id ";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.Parameters.AddWithValue("@payment ", newPayment);
                            cmd.Parameters.AddWithValue("@id ", id);

                            conn.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при обновлении: {ex.Message} ");
                        LoadData();
                    }
                }
                else
                {
                    // Если попытались редактировать другой столбец - отменяем и пока зываем сообщение
                    e.Cancel = true;
                    MessageBox.Show("Редактирование этого поля запрещено ");
                }
            }
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = "SELECT * FROM CheckZakaz ORDER BY id_Check ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgChecks.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message} ");
            }
        }
    }
}