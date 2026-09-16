using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media; // Для Brushes

namespace RestaurantWorkApp.Controls
{
    public partial class ClientControl : UserControl
    {
        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";
        // Ключ для шифрования (должен совпадать с используемым в БД)
        private readonly string encryptionKey = "nfkuser8w4";

        public ClientControl()
        {
            InitializeComponent();
            Loaded += ClientControl_Loaded; // Проверка прав доступа при загрузке
        }

        private void ClientControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();

            if (!AccessControl.CanDoOperation("Client")) // Предполагается, что в AccessControl есть проверка для "Client"
            {
                btnAdd.IsEnabled = false;
                btnDelete.IsEnabled = false;
                dgClients.IsReadOnly = true;
                dgClients.CanUserAddRows = false;
                dgClients.CanUserDeleteRows = false;
                txtFIO_C.IsEnabled = false;
                txtTel_C.IsEnabled = false;
                txtMail.IsEnabled = false;
                dpData_Reg.IsEnabled = false;
                txtLogin.IsEnabled = false;
                pbPassword.IsEnabled = false;
                btnTogglePassword.IsEnabled = false;
                txtIdRule.IsEnabled = false;
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Client"))
            {
                MessageBox.Show("У вас нет прав для добавления клиента!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string fio = txtFIO_C.Text.Trim();
            string tel = txtTel_C.Text.Trim();
            string mail = txtMail.Text.Trim();
            DateTime? regDate = dpData_Reg.SelectedDate;
            string login = txtLogin.Text.Trim();
            string password = pbPassword.Password;
            string idRuleStr = txtIdRule.Text.Trim();

            if (string.IsNullOrEmpty(fio))
            {
                MessageBox.Show("Введите ФИО клиента ");
                return;
            }
            if (string.IsNullOrEmpty(tel))
            {
                MessageBox.Show("Введите телефон ");
                return;
            }
            if (string.IsNullOrEmpty(mail))
            {
                MessageBox.Show("Введите email ");
                return;
            }
            if (regDate == null)
            {
                MessageBox.Show("Выберите дату регистрации ");
                return;
            }
            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Введите логин ");
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите пароль ");
                return;
            }
            if (string.IsNullOrEmpty(idRuleStr) || !int.TryParse(idRuleStr, out int idRule))
            {
                MessageBox.Show("Введите корректный ID роли (целое число) ");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    // Используе м AES_ENCRYPT для шифрования пароля
                    string query = @"INSERT INTO Client (FIO_C, Tel_C, Mail, Data_Reg, login, password, id_rule) 
                                     VALUES(@fio, @tel, @mail, @regDate, @login, AES_ENCRYPT(@password, @key), @idRule) ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@fio", fio);
                    cmd.Parameters.AddWithValue("@tel", tel);
                    cmd.Parameters.AddWithValue("@mail", mail);
                    cmd.Parameters.AddWithValue("@regDate", regDate.Value);
                    cmd.Parameters.AddWithValue("@login", login);
                    cmd.Parameters.AddWithValue("@password", password);
                    cmd.Parameters.AddWithValue("@key", encryptionKey);
                    cmd.Parameters.AddWithValue("@idRule", idRule);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    // Очистка полей
                    txtFIO_C.Clear();
                    txtTel_C.Clear();
                    txtMail.Clear();
                    dpData_Reg.Text = " ";
                    txtLogin.Clear();
                    pbPassword.Password = " ";
                    txtIdRule.Clear();

                    LoadData();
                    MessageBox.Show("Клиент добавлен успешно! ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении клиента: {ex.Message} ");
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Client"))
            {
                MessageBox.Show("У вас нет прав для удаления клиента!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgClients.SelectedItem == null)
            {
                MessageBox.Show("Выберите клиента для удаления ");
                return;
            }

            DataRowView selectedRow = dgClients.SelectedItem as DataRowView;
            int id = Convert.ToInt32(selectedRow["id_Client"]);
            string fio = selectedRow["FIO_C"].ToString();

            var result = MessageBox.Show($"Удалить клиента '{fio}'? ",
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
                                string deleteQuery = "DELETE FROM Client WHERE id_Client = @id";
                                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                                deleteCmd.Parameters.AddWithValue("@id", id);
                                int rowsAffected = deleteCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // 2. Сброс AUTO_INCREMENT
                                    string getMaxIdQuery = "SELECT MAX(id_Client) as max_id FROM Client";
                                    MySqlCommand getMaxIdCmd = new MySqlCommand(getMaxIdQuery, conn, transaction);
                                    object maxIdResult = getMaxIdCmd.ExecuteScalar();

                                    int newAutoIncrementValue = 1;
                                    if (maxIdResult != null && maxIdResult != DBNull.Value)
                                    {
                                        newAutoIncrementValue = Convert.ToInt32(maxIdResult) + 1;
                                    }

                                    string alterQuery = "ALTER TABLE Client AUTO_INCREMENT = @new_auto_increment_value";
                                    MySqlCommand alterCmd = new MySqlCommand(alterQuery, conn, transaction);
                                    alterCmd.Parameters.AddWithValue("@new_auto_increment_value", newAutoIncrementValue);
                                    alterCmd.ExecuteNonQuery();

                                    transaction.Commit();
                                    LoadData();
                                    MessageBox.Show("Клиент удалён успешно! ");
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Ошибка: Клиент не найден или не был удалён.");
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
                    MessageBox.Show($"Ошибка при удалении клиента: {ex.Message} ");
                }
            }
        }

        private void dgClients_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Client"))
            {
                MessageBox.Show("У вас нет прав для редактирования клиента!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataRowView rowView = e.Row.Item as DataRowView;
                if (rowView == null) return;

                int id = Convert.ToInt32(rowView["id_Client"]);
                string columnHeader = (e.Column as DataGridColumn)?.Header.ToString();

                if (string.IsNullOrEmpty(columnHeader)) return;

                // Получаем новое  значение из редактируемого элемента
                string newValue = (e.EditingElement as TextBox)?.Text.Trim();
                if (string.IsNullOrEmpty(newValue) && columnHeader != "Пароль " && columnHeader != "Email ") // некоторые поля могут быть пустыми?
                {
                    // Для простоты разрешим пустые email и пароль? Лучше запретить пустые для обязательных полей.
                    // Согласно схеме: FIO_C NOT NULL, Tel_C NOT NULL, Data_Reg NOT NULL, login NOT NULL, password NOT NULL (хотя password может быть null? в схеме нет NOT NULL, но обычно есть)
                    // Будем считать, что все поля, кроме id_Client и возможно Mail, обязательны.
                    if (columnHeader == "ФИО " || columnHeader == "Телефон " || columnHeader == "Логин " || columnHeader == "Пароль " || columnHeader == "ID роли ")
                    {
                        MessageBox.Show("Значение не может быть пустым ");
                        e.Cancel = true;
                        return;
                    }
                }

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        string fieldName = " ";
                        MySqlCommand cmd;

                        switch (columnHeader)
                        {
                            case "ФИО ":
                                fieldName = "FIO_C";
                                break;
                            case "Телефон ":
                                fieldName = "Tel_C";
                                break;
                            case "Email ":
                                fieldName = "Mail";
                                break;
                            case "Дата рег. ":
                                fieldName = "Data_Reg";
                                // Проверим, что дата корректна
                                if (!DateTime.TryParse(newValue, out _))
                                {
                                    MessageBox.Show("Неверный формат даты ");
                                    e.Cancel = true;
                                    return;
                                }
                                break;
                            case "Логин ":
                                fieldName = "login ";
                                break;
                            case "Пароль ":
                                // Для пароля нужно использовать AES_ENCRYPT при обновлении
                                fieldName = "password ";
                                // Специальный запрос с шифрованием
                                string updatePasswordQuery = "UPDATE Client SET password = AES_ENCRYPT(@value, @key) WHERE id_Client = @id";
                                cmd = new MySqlCommand(updatePasswordQuery, conn);
                                cmd.Parameters.AddWithValue("@value ", newValue);
                                cmd.Parameters.AddWithValue("@key", encryptionKey);
                                cmd.Parameters.AddWithValue("@id", id);
                                conn.Open();
                                cmd.ExecuteNonQuery();
                                // После обновления возвращаемся, чтобы не выполнять  обычный UPDATE
                                return;
                            case "ID роли ":
                                fieldName = "id_rule ";
                                if (!int.TryParse(newValue, out _))
                                {
                                    MessageBox.Show("ID роли должно быть целым числом ");
                                    e.Cancel = true;
                                    return;
                                }
                                break;
                            default:
                                // Неизвестный столбец (например, ID) – не редактируем
                                e.Cancel = true;
                                return;
                        }

                        // Обычное обновление для не-парольных полей
                        string query = $"UPDATE Client SET {fieldName} = @value WHERE id_Client = @id";
                        cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@value ", newValue);
                        cmd.Parameters.AddWithValue("@id", id);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении: {ex.Message} ");
                    LoadData(); // перезагружаем данные в случае ошибки
                }
            }
        }

        private void btnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Client"))
            {
                MessageBox.Show("У вас нет прав для просмотра пароля!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var toggle = sender as ToggleButton;
            if (toggle != null)
            {
                if (toggle.IsChecked == true)
                {
                    // Показываем пароль
                    pbPassword.PasswordChar = '\0'; // символ null = отображать обычный текст
                }
                else
                {
                    // Скрываем пароль
                    pbPassword.PasswordChar = '●';
                }
            }
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    // Расшифровываем пароль при выборке, чтобы отображать его как обычный текст
                    string query = @"SELECT id_Client, FIO_C, Tel_C, Mail, Data_Reg, login, 
                                    CAST(AES_DECRYPT(password, @key) AS CHAR) AS password_plain,
                                     id_rule
                             FROM Client
                             ORDER BY id_Client";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@key", encryptionKey);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgClients.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message} ");
            }
        }
    }
}