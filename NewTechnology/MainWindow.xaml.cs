using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace NewTechnology
{
    public partial class MainWindow : Window
    {
        private NewTechnologyEntities db = new NewTechnologyEntities();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadApplications();
        }

        private void LoadApplications()
        {
            try
            {
                db = new NewTechnologyEntities();
                var applicationsData = new List<ApplicationViewModel>();

                // Получаем ВСЕ заявки
                var allApplications = db.Заявка.ToList();
                var allProducts = db.Продукция.ToList();
                var allPartners = db.Партнеры.ToList();
                var allPartnerTypes = db.ТипПартнеров.ToList();

                foreach (var application in allApplications)
                {
                    var partner = allPartners.FirstOrDefault(p => p.Код == application.КодПартнера);
                    var product = allProducts.FirstOrDefault(p => p.Код == application.КодПродукции);

                    if (partner != null && product != null)
                    {
                        var partnerType = allPartnerTypes.FirstOrDefault(t => t.Код == partner.КодТипПартнера);

                        // Рассчитываем стоимость
                        decimal totalCost = 0;
                        if (product.МинСтоимость.HasValue)
                        {
                            totalCost = (decimal)(product.МинСтоимость.Value * application.КоличествоПродукции);
                        }

                        applicationsData.Add(new ApplicationViewModel
                        {
                            Id = application.Код, // ID заявки
                            PartnerName = partner.Наименование,
                            PartnerType = partnerType?.Наимнование ?? "Неизвестный тип",
                            LegalAddress = partner.ЮрАдрес,
                            PhoneNumber = FormatPhoneNumber(partner.Телефон),
                            Rating = partner.Рейтинг ?? 0,
                            TotalCost = totalCost,
                            ProductName = product.Наименование,
                            Quantity = application.КоличествоПродукции ?? 0
                        });
                    }
                }

                applicationsData = applicationsData.OrderByDescending(a => a.Id).ToList();
                applicationsList.ItemsSource = applicationsData;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        private string FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return "Телефон не указан";
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.Length == 10)
                return $"+7 {digits.Substring(0, 3)} {digits.Substring(3, 3)}-{digits.Substring(6, 2)}-{digits.Substring(8, 2)}";
            if (digits.Length == 11 && digits.StartsWith("7"))
                return $"+7 {digits.Substring(1, 3)} {digits.Substring(4, 3)}-{digits.Substring(7, 2)}-{digits.Substring(9, 2)}";
            return phone;
        }

        private void applicationsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (applicationsList.SelectedItem is ApplicationViewModel vm)
            {
                // Получаем ID партнера из заявки
                var application = db.Заявка.FirstOrDefault(a => a.Код == vm.Id);
                if (application != null)
                {
                    var edit = new EditApplication((int)application.КодПартнера); // Передаем ID партнера
                    edit.ShowDialog();
                    LoadApplications();
                }
            }
        }

        private void productsBtn_Click(object sender, RoutedEventArgs e)
        {
            Products products = new Products();
            products.ShowDialog();

        }

        private void calculationProducts_Btn_Click(object sender, RoutedEventArgs e)
        {
            Calculation calculation = new Calculation();
            calculation.ShowDialog();
        }

        private void addApplicationBtn_Click(object sender, RoutedEventArgs e)
        {
            // Открываем форму добавления нового партнера (без ID)
            var edit = new EditApplication();
            edit.ShowDialog();

            // Если партнер был добавлен, перезагружаем заявки
            if (edit.DialogResult == true)
            {
                LoadApplications();
            }
        }
    }

    // Модель для отображения в MainWindow
    public class ApplicationViewModel
    {
        public int Id { get; set; } // ID заявки
        public string PartnerName { get; set; }
        public string PartnerType { get; set; }
        public string LegalAddress { get; set; }
        public string PhoneNumber { get; set; }
        public int Rating { get; set; }
        public decimal TotalCost { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }

        public string CostText => $"{TotalCost:N2} р".Replace(",", " ");
    }
}