using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Diagnostics;

namespace PortfolioVisualizer.Data
{
    public enum OrderType
    {
        Buy,
        Sell
    }

    public class StockData
    {
        public string TickerName { get; set; }

        public uint LTP { get; set; }
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
                await OnStockTick();
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
                await _ws.ConnectAsync(new Uri("ws://localhost:6666/ws"),
                    CancellationToken.None);

				var buffer = new byte[1024];
                while (true)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer),
                        CancellationToken.None);

                    var strData = Encoding.ASCII.GetString(buffer);

                    var stockArr = strData.Split(':');

                    StockData data = new StockData() { TickerName = stockArr[0],LTP = uint.Parse(stockArr[1]) };

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
