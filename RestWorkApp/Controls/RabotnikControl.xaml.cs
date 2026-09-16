using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Для Brushes

namespace RestaurantWorkApp.Controls
{
    public partial class RabotnikControl : UserControl
    {
        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";

        public RabotnikControl()
        {
            InitializeComponent();
            Loaded += RabotnikControl_Loaded; // Проверка прав доступа при загрузке
        }

        private void RabotnikControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();

            if (!AccessControl.CanDoOperation("Rabotnik")) // Предполагается, что в AccessControl есть проверка для "Rabotnik"
            {
                btnAdd.IsEnabled = false;
                btnDelete.IsEnabled = false;
                dgRabotnik.IsReadOnly = true;
                dgRabotnik.CanUserAddRows = false;
                dgRabotnik.CanUserDeleteRows = false;
                txtFIO.IsEnabled = false;
                txtTel.IsEnabled = false;
                txtLogin.IsEnabled = false;
                txtPassword.IsEnabled = false;
                txtDolhId.IsEnabled = false;
                txtRuleId.IsEnabled = false;
            }
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    // Исключаем пароль из выборки для безопасности (VARBINARY отображается как System.Byte[])
                    string query = "SELECT id_Rabotnik, FIO_R, Tel_R, login, id_dolh, id_rule FROM Rabotnik ORDER BY id_Rabotnik";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgRabotnik.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message} ");
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Rabotnik"))
            {
                MessageBox.Show("У вас нет прав для добавления сотрудника!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string fio = txtFIO.Text.Trim();
            string tel = txtTel.Text.Trim();
            string login = txtLogin.Text.Trim();
            string pass = txtPassword.Text.Trim();
            string dolhStr = txtDolhId.Text.Trim();
            string ruleStr = txtRuleId.Text.Trim();

            if (string.IsNullOrEmpty(fio)) { MessageBox.Show("Введите ФИО сотрудника "); return; }
            if (string.IsNullOrEmpty(tel)) { MessageBox.Show("Введите телефон "); return; }
            if (!int.TryParse(dolhStr, out int idDolh)) { MessageBox.Show("Введите корректный ID должности "); return; }
            if (!int.TryParse(ruleStr, out int idRule)) { MessageBox.Show("Введите корректный ID роли "); return; }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = "INSERT INTO Rabotnik (FIO_R, Tel_R, login, password, id_dolh, id_rule) VALUES (@fio, @tel, @login, @pass, @dolh, @rule) ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@fio", fio);
                    cmd.Parameters.AddWithValue("@tel", tel);
                    cmd.Parameters.AddWithValue("@login", string.IsNullOrEmpty(login) ? (object)DBNull.Value : login);

                    // Преобразуем строку пароля в байты для VARBINARY
                    cmd.Parameters.AddWithValue("@pass", string.IsNullOrEmpty(pass) ? (object)DBNull.Value : Encoding.UTF8.GetBytes(pass));

                    cmd.Parameters.AddWithValue("@dolh", idDolh);
                    cmd.Parameters.AddWithValue("@rule", idRule);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    txtFIO.Clear(); txtTel.Clear(); txtLogin.Clear(); txtPassword.Clear();
                    txtDolhId.Clear(); txtRuleId.Clear();

                    LoadData();
                    MessageBox.Show("Сотрудник добавлен успешно! ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message} ");
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Rabotnik"))
            {
                MessageBox.Show("У вас нет прав для удаления сотрудника!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgRabotnik.SelectedItem == null)
            {
                MessageBox.Show("Выберите сотрудника для удаления ");
                return;
            }

            DataRowView row = dgRabotnik.SelectedItem as DataRowView;
            int id = Convert.ToInt32(row["id_Rabotnik"]);
            string name = row["FIO_R"].ToString();

            var result = MessageBox.Show($"Удалить сотрудника '{name}'?\nВнимание: удалённый сотрудник пропадёт из связанных заказов и доставок. ",
                                           "Подтверждение удаления ",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Warning);

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
                                string deleteQuery = "DELETE FROM Rabotnik WHERE id_Rabotnik = @id";
                                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                                deleteCmd.Parameters.AddWithValue("@id", id);
                                int rowsAffected = deleteCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // 2. Сброс AUTO_INCREMENT
                                    string getMaxIdQuery = "SELECT MAX(id_Rabotnik) as max_id FROM Rabotnik";
                                    MySqlCommand getMaxIdCmd = new MySqlCommand(getMaxIdQuery, conn, transaction);
                                    object maxIdResult = getMaxIdCmd.ExecuteScalar();

                                    int newAutoIncrementValue = 1;
                                    if (maxIdResult != null && maxIdResult != DBNull.Value)
                                    {
                                        newAutoIncrementValue = Convert.ToInt32(maxIdResult) + 1;
                                    }

                                    string alterQuery = "ALTER TABLE Rabotnik AUTO_INCREMENT = @new_auto_increment_value";
                                    MySqlCommand alterCmd = new MySqlCommand(alterQuery, conn, transaction);
                                    alterCmd.Parameters.AddWithValue("@new_auto_increment_value", newAutoIncrementValue);
                                    alterCmd.ExecuteNonQuery();

                                    transaction.Commit();
                                    LoadData();
                                    MessageBox.Show("Сотрудник удалён успешно! ");
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Ошибка: Сотрудник не найден или не был удалён.");
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

        private void dgRabotnik_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Rabotnik"))
            {
                MessageBox.Show("У вас нет прав для редактирования сотрудника!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataRowView row = e.Row.Item as DataRowView;
                if (row == null) return;

                int id = Convert.ToInt32(row["id_Rabotnik"]);
                string header = e.Column.Header as string;
                string newValue = (e.EditingElement as TextBox)?.Text.Trim();

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        string query = " ";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", id);

                        switch (header)
                        {
                            case "ФИО ":
                                if (string.IsNullOrEmpty(newValue)) { MessageBox.Show("ФИО не может быть пустым "); e.Cancel = true; return; }
                                query = "UPDATE Rabotnik SET FIO_R = @val WHERE id_Rabotnik = @id";
                                cmd.Parameters.AddWithValue("@val", newValue);
                                break;

                            case "Телефон ":
                                if (string.IsNullOrEmpty(newValue)) { MessageBox.Show("Телефон не может быть пустым "); e.Cancel = true; return; }
                                query = "UPDATE Rabotnik SET Tel_R = @val WHERE id_Rabotnik = @id";
                                cmd.Parameters.AddWithValue("@val", newValue);
                                break;

                            case "Логин ":
                                query = "UPDATE Rabotnik SET login = @val WHERE id_Rabotnik = @id";
                                cmd.Parameters.AddWithValue("@val", string.IsNullOrEmpty(newValue) ? (object)DBNull.Value : newValue);
                                break;

                            case "ID Должности ":
                                if (!int.TryParse(newValue, out int d)) { MessageBox.Show("Неверный формат ID должности "); e.Cancel = true; return; }
                                query = "UPDATE Rabotnik SET id_dolh = @val WHERE id_Rabotnik = @id";
                                cmd.Parameters.AddWithValue("@val", d);
                                break;

                            case "ID Роли ":
                                if (!int.TryParse(newValue, out int r)) { MessageBox.Show("Неверный формат ID роли "); e.Cancel = true; return; }
                                query = "UPDATE Rabotnik SET id_rule = @val WHERE id_Rabotnik = @id";
                                cmd.Parameters.AddWithValue("@val", r);
                                break;

                            default: return; // ID сотрудника не редактируем
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