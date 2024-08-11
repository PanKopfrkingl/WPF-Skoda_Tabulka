using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Skoda_tabulka
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataGridXml.AutoGeneratingColumn += DataGridXml_AutoGeneratingColumn;
        }

        private void SelectXmlFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                string filename = openFileDialog.FileName;

                DataSet dataSet = new DataSet();
                try
                {
                    dataSet.ReadXml(filename);

                    if (dataSet.Tables.Count > 0)
                    {
                        DataTable dataTable = dataSet.Tables[0];

                        
                        DataTable convertedTable = ConvertColumnsToAppropriateTypes(dataTable);

                        
                        DataGridXml.ItemsSource = convertedTable.DefaultView;

                       
                        DataTable aggregatedTable = CalculateAggregatedData(convertedTable);

                       
                        DataGridAggregated.ItemsSource = aggregatedTable.DefaultView;
                    }
                    else
                    {
                        MessageBox.Show("No data found in the XML file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading XML file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("No file selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataTable ConvertColumnsToAppropriateTypes(DataTable dataTable)
        {
            DataTable convertedTable = dataTable.Clone();
            convertedTable.Columns["Cena"]!.DataType = typeof(double);
            convertedTable.Columns["DPH"]!.DataType = typeof(double);
            convertedTable.Columns["DatumProdeje"]!.DataType = typeof(DateTime);

            foreach (DataRow row in dataTable.Rows)
            {
                convertedTable.ImportRow(row);
            }

            return convertedTable;
        }

        private DataTable CalculateAggregatedData(DataTable dataTable)
        {
            var groupedData = from row in dataTable.AsEnumerable()
                              let model = row.Field<string>("Model")
                              let cena = row.Field<double>("Cena")
                              let dph = row.Field<double>("DPH")
                              let datumProdeje = row.Field<DateTime>("DatumProdeje")
                              where datumProdeje.DayOfWeek == DayOfWeek.Saturday || datumProdeje.DayOfWeek == DayOfWeek.Sunday
                              group new { cena, dph } by model into modelGroup
                              select new
                              {
                                  Model = modelGroup.Key,
                                  CenaBezDPH = modelGroup.Sum(x => x.cena),
                                  CenaSDPH = modelGroup.Sum(x => x.cena + (x.cena * (x.dph / 100)))
                              };

            DataTable aggregatedTable = new DataTable();
            aggregatedTable.Columns.Add("Model", typeof(string));
            aggregatedTable.Columns.Add("CenaBezDPH", typeof(double));
            aggregatedTable.Columns.Add("CenaSDPH", typeof(double));

            foreach (var item in groupedData)
            {
                aggregatedTable.Rows.Add(item.Model, item.CenaBezDPH, item.CenaSDPH);
            }

            return aggregatedTable;
        }

        private void DataGridXml_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(DateTime))
            {
                (e.Column as DataGridTextColumn)!.Binding.StringFormat = "MM/dd/yyyy";
            }
        }

    }
}
