using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using System.Windows.Shapes;

namespace BotanicGarden
{
    /// <summary>
    /// Логика взаимодействия для Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {
        private string connectionString = "Server=AZAZI;Database=Plants;Trusted_Connection=True;";
        public Window2()
        {
            InitializeComponent();
        }
        private int GetCustomerIdByEmail(string email)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT CustomerID FROM Customers WHERE Email = @Email";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);

                    object result = command.ExecuteScalar();
                    if (result != null && result is int)
                    {
                        return (int)result;
                    }
                    return -1; // Если не найдено
                }
            }
        }

        private void ConfirmEmailButton_Click_1(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;

            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Пожалуйста, введите ваш email.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Получаем ID клиента по введенному email
            int customerId = GetCustomerIdByEmail(email);

            if (customerId == -1)
            {
                MessageBox.Show("Клиент с таким email не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Получаем заказы для клиента
            string ordersInfo = GetCustomerOrdersInfo(customerId);

            // Показываем информацию о заказах в MessageBox
            MessageBox.Show(ordersInfo, "Заказы клиента", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private string GetCustomerOrdersInfo(int customerId)
        {
            List<string> orders = new List<string>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
                SELECT o.OrderID, o.OrderDate, o.TotalPrice
                FROM Orders o
                WHERE o.CustomerID = @CustomerID";
               

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerID", customerId);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        orders.Add($"Заказ №{reader.GetInt32(0)} | Дата: {reader.GetDateTime(1):dd/MM/yyyy} | Сумма: {reader.GetDecimal(2):C}");
                    }
                }
            }

            // Формируем строку для отображения в MessageBox
            if (orders.Count > 0)
            {
                return string.Join("\n", orders);
            }
            else
            {
                return "У клиента нет заказов.";
            }
        }
    }
}
