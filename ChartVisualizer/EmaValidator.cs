using CefSharp.DevTools.Cast;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using Trady;
using Trady.Core.Period;
using Trady.Analysis.Infrastructure;
using Trady.Core.Infrastructure;
using Trady.Analysis.Extension;
using Trady.Importer.Yahoo;
using System.Threading;
using StooqApi;
using System.Globalization;
using System.Security.Cryptography;
using Trady.Analysis;
using System.Diagnostics;
using CefSharp.DevTools.Network;
using System.Runtime.Serialization.Json;
using System.Windows;
using Flurl.Util;

namespace ChartVisualizer
{
    public interface ImporterBase
    {
        public List<Trady.Core.Candle> ImportAsync(string symbol, string instrument, DateTime? startTime = null, DateTime? endTime = null, PeriodOption period = PeriodOption.Daily, CancellationToken token = default(CancellationToken), bool Save = false);
    }
    public class MockDataImporter: ImporterBase
    {
        public List<Trady.Core.Candle> ImportAsync(string symbol, string instrument, DateTime? startTime = null, DateTime? endTime = null, PeriodOption period = PeriodOption.Daily, CancellationToken token = default(CancellationToken), bool Save = false)
        {
            if (startTime == null) startTime = DateTime.Now;
            if (endTime == null) endTime = ((DateTime)startTime).AddDays(-80);

            if (period != PeriodOption.Daily)
            {
                throw new ArgumentException("This importer only supports daily data");
            }

            var mockFileName = $@"BTDataDaily\{instrument}.txt";
            if(System.IO.File.Exists(mockFileName))
            {
                var content = System.IO.File.ReadAllText(mockFileName);
                var candles = JsonConvert.DeserializeObject<List<Trady.Core.Candle>> (content);
                return candles;
            }
            return new List<Trady.Core.Candle>();
        }
    }

    public class ZerodhaKiteImporter : ImporterBase
    {

        private Object _lock = new Object();
        public class Data
        {
            public List<List<string>> candles { get; set; }
        }

        public class TickDataDownloadResponse
        {
            public string status { get; set; }
            public Data data { get; set; }
        }


        private (string, string) PrepareUrlBeforeDownload(string startDate, string endDate, string stockId, string stockIdInternal, string timeFrame)
        {
            const string userid = "XRL263";
            var baseUrl =
                  "https://kite.zerodha.com/oms/instruments/historical/STOCK_ID_INTERNAL/TIME_FRAME_INTERNAL?user_id=SECRET_USER_ID&oi=1&from=START_DATE&to=END_DATE";
            //"https://kite.zerodha.com/oms/instruments/historical/STOCK_ID_INTERNAL/minute?user_id=SECRET_USER_ID&oi=1&from=START_DATE&to=END_DATE";

            var referer =
                "https://kite.zerodha.com/chart/ext/tvc/NSE/STOCK_ID/STOCK_ID_INTERNAL";

            baseUrl = baseUrl.Replace("STOCK_ID_INTERNAL", stockIdInternal);
            baseUrl = baseUrl.Replace("SECRET_USER_ID", userid);
            baseUrl = baseUrl.Replace("START_DATE", startDate);
            baseUrl = baseUrl.Replace("END_DATE", endDate);
            baseUrl = baseUrl.Replace("TIME_FRAME_INTERNAL", timeFrame);

            referer = referer.Replace("STOCK_ID_INTERNAL", stockIdInternal);
            referer = referer.Replace("STOCK_ID", stockId);

            return (baseUrl, referer);
        }

        protected List<List<string>> DownloadStockData(string stockName, string stockIdInternal, string startDate, string endDate, string timeFrameStr)
        {
            var baseUrlAndReferer = PrepareUrlBeforeDownload(startDate, endDate, stockName, stockIdInternal, timeFrameStr);
            var response = DownloadInternal(baseUrlAndReferer.Item1, baseUrlAndReferer.Item2);
            //Thread.Sleep(100);
            if(!response.IsSuccessful)
            {
                MessageBox.Show(response.ToString());
            }
            var deserializedResponse = JsonConvert.DeserializeObject<TickDataDownloadResponse>(response.Content);
            if (deserializedResponse.status != "success") throw new DeserializationException(response, new Exception(deserializedResponse.status));
            return deserializedResponse.data.candles;
        }

        private KeyValuePair<string, string> ReadAuthInformation()
        {
            var lines = System.IO.File.ReadAllLines(@"authorization.txt");
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
            var reqURL = client.BuildUri(request);
            return response;
        }

        private static readonly IDictionary<PeriodOption, Period> PeriodMap = new Dictionary<PeriodOption, Period>
        {
            {
                PeriodOption.Daily,
                Period.Daily
            },
            {
                PeriodOption.Weekly,
                Period.Weekly
            },
            {
                PeriodOption.Monthly,
                Period.Monthly
            }
        };

        public List<Trady.Core.Candle> ImportAsync(string symbol, string instrument, DateTime? startTime = null, DateTime? endTime = null, PeriodOption period = PeriodOption.Daily, CancellationToken token = default(CancellationToken), bool Save = false)
        {
            if (startTime == null) startTime = DateTime.Now;
            if (endTime == null) endTime = ((DateTime)startTime).AddDays(-80);

            if (period != PeriodOption.Daily)
            {
                throw new ArgumentException("This importer only supports daily data");
            }

            var startDateStr = ((DateTime)startTime).ToString("yyyy-MM-dd");
            var endDateStr = ((DateTime)endTime).ToString("yyyy-MM-dd");

            var dirPath = $@"BTDataDaily\{((DateTime)startTime).ToString("yyyy-MM-dd")}";
            if(!Directory.Exists(dirPath))
            {
               Directory.CreateDirectory(dirPath);
            }

            var instrumentPath = Path.Combine(dirPath, instrument + ".txt");
            if (!System.IO.File.Exists(instrumentPath))
            {
                try
                {
                    List<List<string>> candles;
                    lock (_lock)
                    {
                    //    Random r = new Random();
                    //int x = r.Next(1000,2000);
                    //while (x < 999) ++x;
                    candles = DownloadStockData(symbol, instrument, endDateStr, startDateStr, "day");
                    Thread.Sleep(80);

                    }

                    List<Trady.Core.Candle> candleSticks = new();
                    foreach (var tickData in candles)
                    {
                        candleSticks.Add(new Trady.Core.Candle(
                            DateTime.ParseExact(tickData[0], "yyyy-MM-ddTHH:mm:ss+0530", CultureInfo.InvariantCulture),
                            decimal.Parse(tickData[1]),
                            decimal.Parse(tickData[2]),
                            decimal.Parse(tickData[3]),
                            decimal.Parse(tickData[4]),
                            decimal.Parse(tickData[5])));
                    }
                    var candlesSorted = candleSticks.OrderBy(item => item.DateTime).ToList();
                    System.IO.File.WriteAllText(instrumentPath, JsonConvert.SerializeObject(candlesSorted));
                    return candlesSorted;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    throw;
                }
            }
            else
            {
                var content = System.IO.File.ReadAllText(instrumentPath);
                var candles = JsonConvert.DeserializeObject<List<Trady.Core.Candle>>(content);
                return candles;
            }
        }
    }
    internal class ValidatorBase
    {
        protected ImporterBase mImporter;
        internal ValidatorBase(ImporterBase importer)
        {
            mImporter = importer;
        }
    }
    internal class EmaValidator : ValidatorBase
    {
        public EmaValidator(ImporterBase importer) : base(importer){ }
        public (bool, string) IsSelectedAsync(string stockName, string stockIdInternal, DateTime? startDate = null, DateTime? endDate = null, string timeFrameStr = "", bool save = false)
        {
            var prices = mImporter.ImportAsync(stockName, stockIdInternal, startDate, endDate, PeriodOption.Daily, default(CancellationToken), save);
            var emaPrices = prices.Select(item => item.Close).ToList().Ema(20);

            if (prices.Count == 0) { return (false, string.Empty); }
            int searchIdx = -1;
            decimal correctionFactor = 1;

            // if price is above 2% of ema, ignore.
            decimal latestCloseprice = prices[prices.Count - 1].Close;
            decimal lastEma = (decimal)emaPrices[prices.Count - 1];

            if (Math.Abs(latestCloseprice - lastEma) > (decimal)latestCloseprice * 0.02M)
                return (false, string.Empty);

            for (int i = prices.Count - 1; i >= 0; --i)
            {

                if (emaPrices[i] * correctionFactor < prices[i].Low)
                {
                    searchIdx = i;
                }
                else
                {
                    break;
                }

            }

            if (searchIdx == -1) { return (false, string.Empty); }
            decimal previousPeak = decimal.MinValue;
            int diff = 0;
            for (int i = prices.Count - 1; i >= searchIdx; --i)
            {
                if (prices[i].High > previousPeak)
                {
                    previousPeak = prices[i].High;
                    diff = prices.Count - 1 - i;
                }
            }
            if (diff < 4) return (false, string.Empty);


            if (prices[prices.Count - 1].High < previousPeak)
            {
                Debug.WriteLine($"{stockName} ##### {((DateTime)startDate).ToString("yyyy-MM-dd")} pivot at {previousPeak}. valid criteria...#######################");

                HighChartResponse response = new HighChartResponse();
                response.Prices = new List<List<decimal>>();
                response.Volumes = new List<List<decimal>>();
                response.EMA20 = new List<List<decimal>>();
                response.Pivot = new List<List<decimal>>();
                response.StockName = stockName;

                for (int i = 0; i < prices.Count; ++i)
                {
                    response.Prices.Add(new List<decimal> { prices[i].DateTime.ToUnixTimeMilliseconds(), prices[i].Open, prices[i].High, prices[i].Low, prices[i].Close });
                    response.Volumes.Add(new List<decimal> { prices[i].DateTime.ToUnixTimeMilliseconds(), prices[i].Volume });
                    response.EMA20.Add(new List<decimal> { prices[i].DateTime.ToUnixTimeMilliseconds(), (decimal)emaPrices[i] });
                    response.Pivot.Add(new List<decimal> { prices[i].DateTime.ToUnixTimeMilliseconds(), (decimal)previousPeak });
                }

                return (true, JsonConvert.SerializeObject(response, Formatting.Indented));
            }
            return (false, string.Empty);
        }

        public class HighChartResponse
        {
            public List<List<decimal>> Prices { get; set; }
            public string StockName { get; set; }
            public List<List<decimal>> Volumes { get; set; }
            public List<List<decimal>> EMA20 { get; set; }
            public List<List<decimal>> Pivot { get; set; }

        }
    }
}
