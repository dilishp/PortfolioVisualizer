using CefSharp;
using CefSharp.DevTools.Network;
using CefSharp.Wpf;
using CsvHelper;
using Microsoft.CodeAnalysis;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChartVisualizer
{
    public class InstrumentInfo
    {
        public string instrument_token { get; set; }
        public string exchange_token { get; set; }
        public string tradingsymbol { get; set; }
        public string name { get; set; }
        public string last_price { get; set; }
        public string expiry { get; set; }
        public string strike { get; set; }
        public string tick_size { get; set; }
        public string lot_size { get; set; }
        public string instrument_type { get; set; }
        public string segment { get; set; }
        public string exchange { get; set; }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string mResponse = string.Empty;

        public ObservableCollection<string> Stocks = new ObservableCollection<string>();
        public ConcurrentBag<string> StocksList { get; set; } = new ConcurrentBag<string>();
        public ConcurrentDictionary<string, string> StockDataMap { get; set; } = new ConcurrentDictionary<string, string>();
        public MainWindow()
        {
            InitializeComponent();
            //var settings = new CefSettings();

            Loaded += MainWindow_Loaded;
        }

        private KeyValuePair<string, string> ReadAuthInformation()
        {
            var lines = File.ReadAllLines(@"authorization.txt");
            return new KeyValuePair<string, string>(lines[0], lines[1]);
        }
        private IRestResponse DownloadInternal(string baseUrl, string referer)
        {
            var client = new RestClient(baseUrl) { Timeout = -1 };

            var request = new RestRequest(Method.GET);
            var auth = ReadAuthInformation();
            request.AddHeader("authority", "kite.zerodha.com");
            request.AddHeader("accept", "application/json, text/plain, */*");

            request.AddHeader("authorization", auth.Key);
            request.AddHeader("cookie", auth.Value
                );

            client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36";
            request.AddHeader("sec-fetch-site", "same-origin");
            request.AddHeader("sec-fetch-mode", "cors");
            request.AddHeader("sec-fetch-dest", "empty");

            request.AddHeader("accept-language", "en-US,en;q=0.9");
            request.AddHeader("dnt", "1");

            request.AddHeader("referer", referer);
            request.AddHeader("sec-ch-ua", "\"Chromium\";v=\"110\", \"Not A(Brand\";v=\"24\", \"Microsoft Edge\";v=\"110\"");
            request.AddHeader("sec-ch-ua-mobile", "?0");
            request.AddHeader("sec-ch-ua-platform", "\"Windows\"");
            request.AddHeader("sec-fetch-dest", "empty");
            request.AddHeader("sec-fetch-mode", "cors");
            request.AddHeader("sec-fetch-site", "same-origin");
            IRestResponse response = client.Execute(request);
            return response;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
            var response = DownloadInternal(
           "https://api.kite.trade/instruments",
           "https://kite.zerodha.com/chart/ext/tvc/INDICES/NIFTY%2050/256265");

            List<InstrumentInfo> instruments;


            ImporterBase importer = new ZerodhaKiteImporter();
            EmaValidator validator = new EmaValidator(new ZerodhaKiteImporter());
            
            using (TextReader textReader = new StringReader(response.Content))
            {
                using var csv = new CsvReader(textReader);
                instruments = csv.GetRecords<InstrumentInfo>().ToList();

                if (instruments.Count() > 0)
                {
                    var filteredInstruments = instruments.Where(item => item.exchange == "NSE" && item.segment == "NSE" &&
                    item.instrument_type=="EQ" &&  string.IsNullOrEmpty(item.name) == false).ToList();
                    bool SaveToLocal = false;

                    if(importer.GetType() == typeof(ZerodhaKiteImporter))
                    {
                        foreach(var instr in filteredInstruments)
                        {
                            var optionInstrument = instr.instrument_token;
                            bool valid = false;
                            string data = string.Empty;
                            (valid, data) = validator.IsSelectedAsync(instr.tradingsymbol, optionInstrument, DateTime.Now, null, "", SaveToLocal);
                            if (valid)
                            {
                                StocksList.Add(instr.tradingsymbol);
                                StockDataMap.TryAdd(instr.tradingsymbol, data);
                            }
                        }
                    }
                    else if(importer.GetType() == typeof(MockDataImporter))
                    {
                        Parallel.ForEach(filteredInstruments, instr =>
                        {
                            var optionInstrument = instr.instrument_token;
                            bool valid = false;
                            string data = string.Empty;
                            (valid, data) = validator.IsSelectedAsync(instr.tradingsymbol, optionInstrument, DateTime.Now, null, "", SaveToLocal);
                            if (valid)
                            {
                                StocksList.Add(instr.tradingsymbol);
                                StockDataMap.TryAdd(instr.tradingsymbol, data);
                            }
                        });
                    }

                    

                    foreach(var item in StocksList)
                    {
                        Stocks.Add(item);
                    }
                    selectedStockList.ItemsSource = Stocks;
                }
            }
            LoadHighstockChart();
        }

        private void LoadHighstockChart()
        {
            browser.LoadHtml(File.ReadAllText(@"web.html"));
            browser.ShowDevTools();
        }

        private void testBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadEMA();
        }

        private void selectedStockList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LoadEMA();
        }

        private void LoadEMA()
        {
            string stockSelected = (string)selectedStockList.SelectedItem;
            if (StockDataMap.ContainsKey(stockSelected))
            {
                string javascriptCode = $"displayMessage({StockDataMap[stockSelected]})";
                browser.ExecuteScriptAsync(javascriptCode);
            }
        }
    }
}
