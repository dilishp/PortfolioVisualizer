using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Diagnostics;

namespace PortfolioVisualizer.Data
{
    public class StockData
    {
        public string TickerName { get; set; }

        public uint TickerPrice { get; set; }
    }

    public class StockTickListenerService
    {
        private ClientWebSocket _ws = new ClientWebSocket();

        public StockTickListenerService() 
        {
            
        }

        public event Action<StockData> StockTickReceived;

        public async Task Listen()
        {
            try
            {
                await Task.WhenAll(
                _ws.ConnectAsync(new Uri("ws://localhost:6666/ws"),
                    CancellationToken.None),
                Task.Factory.StartNew(() => OnStockTick()));
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        public async Task OnStockTick()
        {
            try
            {
                var buffer = new byte[1024];
                while (true)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer),
                        CancellationToken.None);
                    Console.WriteLine(result);

                    StockData data = new StockData();

                    StockTickReceived(data);
                }
            }
            catch(Exception e)
            {
                string msg = e.Message;
            }
        }
    }
}
