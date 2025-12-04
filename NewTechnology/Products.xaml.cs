using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NewTechnology
{
    public partial class Products : Window
    {
        private NewTechnologyEntities db = new NewTechnologyEntities();
        private List<ProductViewModel> allProducts = new List<ProductViewModel>();
        private int? selectedPartnerId = null;

        public Products()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPartners();
            LoadProducts();
        }

        private void LoadPartners()
        {
            try
            {
                // Загружаем всех партнеров
                var partners = db.Партнеры
                    .OrderBy(p => p.Наименование)
                    .Select(p => new { p.Код, p.Наименование })
                    .ToList();

                // Очищаем и добавляем "Все партнеры"
                partnerComboBox.Items.Clear();
                partnerComboBox.Items.Add(new ComboBoxItem
                {
                    Content = "Все партнеры",
                    Tag = 0
                });

                // Добавляем партнеров в комбобокс
                foreach (var partner in partners)
                {
                    partnerComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = partner.Наименование,
                        Tag = partner.Код
                    });
                }

                // Выбираем "Все партнеры" по умолчанию
                partnerComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки партнеров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProducts()
        {
            try
            {
                // Загружаем всю продукцию
                var products = db.Продукция.ToList();
                var applications = db.Заявка.ToList();

                allProducts = new List<ProductViewModel>();

                foreach (var product in products)
                {
                    // Находим заявки для этого продукта (с фильтрацией по партнеру если выбран)
                    var productApplications = applications
                        .Where(a => a.КодПродукции == product.Код);

                    // Если выбран конкретный партнер, фильтруем по нему
                    if (selectedPartnerId.HasValue && selectedPartnerId.Value > 0)
                    {
                        productApplications = productApplications
                            .Where(a => a.КодПартнера == selectedPartnerId.Value);
                    }

                    var applicationsList = productApplications.ToList();
                    int totalQuantity = applicationsList.Sum(a => a.КоличествоПродукции ?? 0);

                    decimal totalCost = 0;
                    if (product.МинСтоимость.HasValue && totalQuantity > 0)
                    {
                        totalCost = (decimal)(product.МинСтоимость.Value * totalQuantity);
                    }

                    allProducts.Add(new ProductViewModel
                    {
                        Код = product.Код,
                        Наименование = product.Наименование ?? "Без названия",
                        МинСтоимость = (decimal)(product.МинСтоимость ?? 0),
                        КоличествоВЗаявках = totalQuantity,
                        ОбщаяСтоимость = totalCost
                    });
                }

                var displayProducts = allProducts;

                if (selectedPartnerId.HasValue && selectedPartnerId.Value > 0)
                {
                    displayProducts = allProducts
                        .Where(p => p.КоличествоВЗаявках > 0)
                        .OrderByDescending(p => p.КоличествоВЗаявках)
                        .ThenBy(p => p.Наименование)
                        .ToList();
                }
                else
                {
                    displayProducts = allProducts
                        .OrderByDescending(p => p.КоличествоВЗаявках)
                        .ThenBy(p => p.Наименование)
                        .ToList();
                }

                productsGrid.ItemsSource = displayProducts;

             
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки продукции: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PartnerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (partnerComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                selectedPartnerId = (int?)selectedItem.Tag;
                LoadProducts();
            }
        }

        private string GetSelectedPartnerName()
        {
            if (partnerComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Content.ToString();
            }
            return "";
        }

        private void ProductsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (productsGrid.SelectedItem is ProductViewModel selectedProduct)
            {
                string details = $"НАИМЕНОВАНИЕ: {selectedProduct.Наименование}\n" +
                               $"Мин. стоимость: {selectedProduct.МинСтоимость:N2} руб.\n" +
                               $"Заказано: {selectedProduct.КоличествоВЗаявках} шт.\n" +
                               $"Общая стоимость заказов: {selectedProduct.ОбщаяСтоимость:N2} руб.";

                MessageBox.Show(details, "Информация о продукте",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Класс для отображения продукции
        public class ProductViewModel
        {
            public int Код { get; set; }
            public string Наименование { get; set; }
            public decimal МинСтоимость { get; set; }
            public int КоличествоВЗаявках { get; set; }
            public decimal ОбщаяСтоимость { get; set; }
        }
    }
}