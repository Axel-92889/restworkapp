using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using MySql.Data.MySqlClient;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Font = iTextSharp.text.Font;
using BaseColor = iTextSharp.text.BaseColor;
using IElement = iTextSharp.text.IElement;
using System.Globalization;

namespace RestaurantWorkApp
{
    public class CheckItem
    {
        public int id_Zakaz { get; set; }
        public string DisplayText { get; set; }
    }

    public partial class ReportsWindow : Window
    {
        private static string ConnStr = "server=localhost;port=3306;username=root;password=root;database=Restaurant_db";
        private DataTable currentData;

        // Цвета для чека
        private static readonly BaseColor RedColor = new BaseColor(220, 53, 69);
        private static readonly BaseColor BlackColor = BaseColor.BLACK;
        private static readonly BaseColor GrayColor = BaseColor.GRAY;
        private static readonly BaseColor LightGray = new BaseColor(248, 249, 250);
        private static readonly BaseColor BorderColor = new BaseColor(222, 226, 230);

        // Шрифты (поля класса для доступа из хелпер-методов)
        private Font _fontTitle, _fontNormal, _fontSmall, _fontBold, _fontTotal;

        public ReportsWindow()
        {
            InitializeComponent();
            Loaded += ReportsWindow_Loaded;
            FontFactory.RegisterDirectories();
        }

        private void ReportsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            if (dpFrom != null && dpTo != null)
            {
                dpFrom.SelectedDate = new DateTime(now.Year, now.Month, 1);
                dpTo.SelectedDate = now;
            }
            if (cmbReportType != null) cmbReportType.SelectedIndex = 0;
            if (cmbPeriod != null) cmbPeriod.SelectedIndex = 0;
        }

        private void cmbReportType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (panelPeriod == null || panelDates == null || cmbPeriod == null || panelCheck == null)
                return;

            int type = cmbReportType.SelectedIndex;
            panelPeriod.Visibility = type == 2 ? Visibility.Collapsed : Visibility.Visible;
            bool customPeriodSelected = cmbPeriod.SelectedIndex == 4;
            panelDates.Visibility = (type != 2 && customPeriodSelected) ? Visibility.Visible : Visibility.Collapsed;
            panelCheck.Visibility = type == 2 ? Visibility.Visible : Visibility.Collapsed;

            if (btnExportExcel != null)
                btnExportExcel.Visibility = type != 2 ? Visibility.Visible : Visibility.Collapsed;
            if (btnPrintPdf != null)
                btnPrintPdf.Visibility = type == 2 ? Visibility.Visible : Visibility.Collapsed;

            if (type == 2)
                LoadChecks();
        }

        private void cmbPeriod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (panelDates == null || cmbPeriod == null) return;
            panelDates.Visibility = cmbPeriod.SelectedIndex == 4 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            int reportType = cmbReportType.SelectedIndex;
            GetDateRange(out DateTime startDate, out DateTime endDate);

            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnStr))
                {
                    conn.Open();

                    if (reportType == 0)
                    {
                        string query = @"
                        SELECT DATE(Data_Z) as 'Дата', 
                               COUNT(*) as 'Кол-во заказов', 
                               SUM(Price_Z) as 'Выручка',
                               ROUND(SUM(Price_Z) * 0.3, 2) as 'Прибыль (30%)'
                        FROM Zakaz 
                        WHERE Status_Z = 'C' AND Data_Z BETWEEN @start AND @end
                        GROUP BY DATE(Data_Z) 
                        ORDER BY Дата DESC";

                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@start", startDate);
                        cmd.Parameters.AddWithValue("@end", endDate);
                        FillDataGrid(cmd);
                    }
                    else if (reportType == 1)
                    {
                        string query = @"
                        SELECT d.Name_Dish as 'Блюдо', 
                               SUM(zi.quantity) as 'Продано шт.', 
                               SUM(zi.quantity * zi.price_at_time) as 'Выручка', 
                               ROUND(AVG(zi.price_at_time), 2) as 'Ср. цена'
                        FROM Zakaz_Items zi
                        JOIN Dish d ON zi.id_Dish = d.id_Dish
                        JOIN Zakaz z ON zi.id_Zakaz = z.id_Zakaz
                        WHERE z.Status_Z = 'C' AND z.Data_Z BETWEEN @start AND @end
                        GROUP BY d.id_Dish, d.Name_Dish
                        ORDER BY `Продано шт.` DESC
                        LIMIT 50";

                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@start", startDate);
                        cmd.Parameters.AddWithValue("@end", endDate);
                        FillDataGrid(cmd);
                    }
                    else if (reportType == 2)
                    {
                        MessageBox.Show("Выберите заказ из списка и нажмите 'Печать'", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        dgPreview.ItemsSource = null;
                        currentData = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (currentData == null || currentData.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                FileName = $"Отчет_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (saveDialog.ShowDialog() != true) return;

            try
            {
                var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Отчёт");

                // Заголовки с оформлением
                for (int i = 0; i < currentData.Columns.Count; i++)
                {
                    var cell = worksheet.Cell(1, i + 1);
                    cell.Value = currentData.Columns[i].ColumnName;
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DC3545");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#DC3545");
                }

                // Данные
                for (int row = 0; row < currentData.Rows.Count; row++)
                {
                    for (int col = 0; col < currentData.Columns.Count; col++)
                    {
                        var cell = worksheet.Cell(row + 2, col + 1);
                        cell.Value = currentData.Rows[row][col]?.ToString() ?? "";

                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.Gray;

                        // Чередование фона
                        if (row % 2 == 0)
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8F9FA");

                        // Выравнивание чисел по правому краю
                        if (col > 0)
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                }

                // Авто-ширина колонок
                worksheet.Columns().AdjustToContents();

                workbook.SaveAs(saveDialog.FileName);
                MessageBox.Show("Отчёт сохранён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка Excel: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPrintPdf_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCheck.SelectedItem == null)
            {
                MessageBox.Show("Выберите заказ для печати!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedOrder = cmbCheck.SelectedItem as CheckItem;
            if (selectedOrder == null)
            {
                MessageBox.Show("Неверный формат заказа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int zakazId = selectedOrder.id_Zakaz;
            if (zakazId <= 0)
            {
                MessageBox.Show("Не удалось получить ID заказа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"Check_{zakazId}_{DateTime.Now:yyyyMMdd}"
            };

            if (saveDialog.ShowDialog() != true) return;

            try
            {
                GenerateCheckPdf(zakazId, saveDialog.FileName);
                MessageBox.Show("Чек создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(saveDialog.FileName)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GetDateRange(out DateTime start, out DateTime end)
        {
            var now = DateTime.Now;
            DateTime? selectedStart = dpFrom?.SelectedDate;
            DateTime? selectedEnd = dpTo?.SelectedDate;

            switch (cmbPeriod.SelectedIndex)
            {
                case 0:
                    start = new DateTime(now.Year, now.Month, 1);
                    end = now.Date.AddDays(1).AddTicks(-1);
                    break;
                case 1:
                    int quarter = (now.Month - 1) / 3 + 1;
                    start = new DateTime(now.Year, (quarter - 1) * 3 + 1, 1);
                    end = start.AddMonths(3).AddTicks(-1);
                    break;
                case 2:
                    start = new DateTime(now.Year, 1, 1);
                    end = new DateTime(now.Year, 12, 31, 23, 59, 59);
                    break;
                case 3:
                    start = new DateTime(2000, 1, 1);
                    end = DateTime.Now;
                    break;
                default:
                    start = selectedStart ?? DateTime.Today.AddDays(-30);
                    end = selectedEnd ?? DateTime.Today;
                    end = end.Date.AddDays(1).AddTicks(-1);
                    break;
            }
        }

        private void FillDataGrid(MySqlCommand cmd)
        {
            using (var adapter = new MySqlDataAdapter(cmd))
            {
                currentData = new DataTable();
                adapter.Fill(currentData);
                dgPreview.ItemsSource = currentData.DefaultView;
            }
        }

        private void LoadChecks()
        {
            try
            {
                using (var conn = new MySqlConnection(ConnStr))
                {
                    conn.Open();
                    string query = @"
                          SELECT z.id_Zakaz, c.FIO_C, z.Price_Z, z.Data_Z 
                          FROM Zakaz z 
                          JOIN Client c ON z.id_client = c.id_Client 
                          WHERE z.Status_Z = 'C' 
                          ORDER BY z.Data_Z DESC 
                          LIMIT 100";

                    using (var adapter = new MySqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        cmbCheck.ItemsSource = dt.AsEnumerable().Select(row => new CheckItem
                        {
                            id_Zakaz = row.Field<int>("id_Zakaz"),
                            DisplayText = $"#{row["id_Zakaz"]} | {row["FIO_C"]} | {row["Price_Z"]:F2} ₽ | {Convert.ToDateTime(row["Data_Z"]):dd.MM.yy HH:mm}"
                        }).ToList();

                        if (cmbCheck.Items.Count > 0)
                            cmbCheck.SelectedIndex = 0;
                        else
                            cmbCheck.ItemsSource = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateCheckPdf(int zakazId, string filePath)
        {
            var ruCulture = new CultureInfo("ru-RU");

            // 1. Загружаем шрифты с поддержкой кириллицы (системные Arial)
            string fontDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            BaseFont bfNormal = BaseFont.CreateFont(Path.Combine(fontDir, "arial.ttf"), BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            BaseFont bfBold = BaseFont.CreateFont(Path.Combine(fontDir, "arialbd.ttf"), BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

            _fontTitle = new Font(bfBold, 12, Font.NORMAL, RedColor);
            _fontNormal = new Font(bfNormal, 9, Font.NORMAL, BaseColor.BLACK);
            _fontSmall = new Font(bfNormal, 7, Font.NORMAL, GrayColor);
            _fontBold = new Font(bfBold, 9, Font.NORMAL, BaseColor.BLACK);
            _fontTotal = new Font(bfBold, 11, Font.NORMAL, RedColor);

            // 2. Размер чека под термо-принтер 80мм
            var doc = new Document(new Rectangle(227f, 800f), 10f, 10f, 10f, 10f);
            PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
            doc.Open();

            string orderInfo = "", clientName = "", cashierName = "", clientEmail = "";
            decimal totalSum = 0;
            DateTime orderDate = DateTime.Now;

            // 3. Данные заказа
            using (var conn = new MySqlConnection(ConnStr))
            {
                conn.Open();
                var query = @"SELECT z.id_Zakaz, z.Data_Z, z.Price_Z, c.FIO_C, c.Mail, r.FIO_R as Staff_Name
                      FROM Zakaz z
                      LEFT JOIN Client c ON z.id_client = c.id_Client
                      LEFT JOIN Rabotnik r ON z.id_rabotnik = r.id_Rabotnik
                      WHERE z.id_Zakaz = @id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", zakazId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            orderDate = Convert.ToDateTime(reader["Data_Z"]);
                            orderInfo = $"КАССОВЫЙ ЧЕК №: {zakazId:D3} {orderDate:dd.MM.yy HH:mm}";
                            clientName = reader["FIO_C"]?.ToString() ?? "Гость";
                            cashierName = reader["Staff_Name"]?.ToString() ?? "Оператор";
                            clientEmail = reader["Mail"]?.ToString() ?? "";
                            totalSum = Convert.ToDecimal(reader["Price_Z"]);
                        }
                    }
                }
            }

            // 4. Заголовок
            doc.Add(new Paragraph($"ООО \"ПИЦЦА ОТ КУНИЦЫ\"\n", _fontTitle) { Alignment = Element.ALIGN_CENTER });
            doc.Add(new Paragraph($"{orderInfo}\nСМЕНА: 001", _fontNormal) { Alignment = Element.ALIGN_CENTER });
            doc.Add(new Paragraph(" ") { SpacingAfter = 6f });

            // 5. Линия
            doc.Add(CreateLine());
            doc.Add(new Paragraph(" ") { SpacingAfter = 6f });

            // 6. Кассир / Клиент
            var staffTable = new PdfPTable(2) { WidthPercentage = 100 };
            staffTable.SetWidths(new float[] { 35, 65 });
            staffTable.DefaultCell.Border = Rectangle.NO_BORDER;
            staffTable.DefaultCell.PaddingBottom = 3;
            staffTable.AddCell(new Phrase("Кассир:", _fontNormal));
            staffTable.AddCell(new Phrase(cashierName, _fontNormal));
            staffTable.AddCell(new Phrase("Клиент:", _fontNormal));
            staffTable.AddCell(new Phrase(clientName, _fontNormal));
            doc.Add(staffTable);
            doc.Add(new Paragraph(" ") { SpacingAfter = 6f });
            doc.Add(CreateLine());
            doc.Add(new Paragraph(" ") { SpacingAfter = 6f });

            // 7. 📦 ОДНА общая таблица товаров (колонки не разъезжаются)
            var itemsTable = new PdfPTable(4) { WidthPercentage = 100 };
            itemsTable.SetWidths(new float[] { 45, 15, 20, 20 });
            itemsTable.DefaultCell.Border = Rectangle.NO_BORDER;
            itemsTable.DefaultCell.PaddingBottom = 4;

            // Заголовки
            itemsTable.AddCell(CreateHeaderCell("Наименование", _fontBold));
            itemsTable.AddCell(CreateHeaderCell("Кол.", _fontBold));
            itemsTable.AddCell(CreateHeaderCell("Цена", _fontBold));
            itemsTable.AddCell(CreateHeaderCell("Сумма", _fontBold));

            // Строки позиций
            using (var conn = new MySqlConnection(ConnStr))
            {
                conn.Open();
                var itemsQuery = @"SELECT d.Name_Dish, zi.quantity, zi.price_at_time,
                                  (zi.quantity * zi.price_at_time) as total
                           FROM Zakaz_Items zi
                           JOIN Dish d ON zi.id_dish = d.id_Dish
                           WHERE zi.id_zakaz = @id";
                using (var cmd = new MySqlCommand(itemsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@id", zakazId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = reader["Name_Dish"].ToString();
                            int qty = Convert.ToInt32(reader["quantity"]);
                            decimal price = Convert.ToDecimal(reader["price_at_time"]);
                            decimal sum = Convert.ToDecimal(reader["total"]);

                            itemsTable.AddCell(new Phrase(name, _fontNormal));
                            itemsTable.AddCell(new Phrase(qty.ToString(), _fontNormal));
                            itemsTable.AddCell(new Phrase(price.ToString("F2", ruCulture), _fontNormal));
                            itemsTable.AddCell(new Phrase(sum.ToString("F2", ruCulture), _fontBold));
                        }
                    }
                }
            }
            doc.Add(itemsTable);
            doc.Add(new Paragraph(" ") { SpacingAfter = 6f });
            doc.Add(CreateLine());
            doc.Add(new Paragraph(" ") { SpacingAfter = 6f });

            // 8. ИТОГО
            doc.Add(new Paragraph($"ИТОГО: {totalSum.ToString("F2", ruCulture)} ₽", _fontTotal) { Alignment = Element.ALIGN_RIGHT });
            doc.Add(new Paragraph(" ") { SpacingAfter = 6f });
            doc.Add(CreateLine());
            doc.Add(CreateLine());
            doc.Add(new Paragraph(" ") { SpacingAfter = 6f });

            // 9. Благодарность
            doc.Add(new Paragraph("Спасибо за Вашу покупку!\nПриятного аппетита!", _fontNormal) { Alignment = Element.ALIGN_CENTER });
            doc.Add(new Paragraph(" ") { SpacingAfter = 6f });
            doc.Add(CreateDottedLine());
            doc.Add(new Paragraph(" ") { SpacingAfter = 6f });

            // 10. Футер
            string footerText = $"Отправитель: au76902@qmail.com  Получатель: {clientEmail}\n" +
                                $"Чек: {DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", ruCulture)}\n" +
                                $"www.martenpizza.ru";
            doc.Add(new Paragraph(footerText, _fontSmall) { Alignment = Element.ALIGN_CENTER });

            doc.Close();
            writer.Close();
        }

        private PdfPCell CreateHeaderCell(string text, Font font)
        {
            return new PdfPCell(new Phrase(text, font))
            {
                Border = Rectangle.BOTTOM_BORDER,
                BorderColor = GrayColor,
                BorderWidthBottom = 0.5f,
                PaddingBottom = 3,
                HorizontalAlignment = Element.ALIGN_LEFT
            };
        }

        private IElement CreateLine()
        {
            var lineTable = new PdfPTable(1) { WidthPercentage = 100 };
            lineTable.DefaultCell.Border = Rectangle.NO_BORDER;
            var line = new PdfPCell(new Phrase(" "))
            {
                Border = Rectangle.TOP_BORDER,
                BorderWidthTop = 1.5f,
                BorderColorTop = BlackColor
            };
            lineTable.AddCell(line);
            return lineTable;
        }

        private IElement CreateDottedLine()
        {
            var dottedTable = new PdfPTable(1) { WidthPercentage = 100 };
            dottedTable.DefaultCell.Border = Rectangle.NO_BORDER;
            var dotted = new PdfPCell(new Phrase(new string('.', 50), _fontSmall))
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_CENTER
            };
            dottedTable.AddCell(dotted);
            return dottedTable;
        }
    }
}