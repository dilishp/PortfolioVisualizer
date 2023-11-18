using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortfolioVisualizer.Data
{
	public class Order
	{
		public string Ticker { get; set; }
		public uint Price { get; set; }
		public uint Quantity { get; set; }
	}

	public class HoldingComparer : IEqualityComparer<Holding>
	{
		public bool Equals(Holding x, Holding y)
		{
			return x.Ticker.Equals(y.Ticker);
		}

		public int GetHashCode([DisallowNull] Holding obj)
		{
			return obj.Ticker.GetHashCode();
		}
	}

	public class Allotment
	{
		public uint AllotPrice { get; set; }
		public int Quantity { get; set; }
	}

	public class Holding
	{
		public Holding()
		{
			Allotments = new List<Allotment>();
			AveragePrice= 0;
			Gain = 0;
			TotalValue = 0;
		}

		public string Ticker { get; set; }
		public uint AveragePrice { get; private set; }
		public IList<Allotment> Allotments { get; set; }
		public  uint TotalValue { get; set; }
		public  uint TotalUnits { get; set; }
		public int Gain { get; set; }
		public uint LTP { get; set; }

		internal void CalculateAverage()
		{
			int sum = 0;
			uint quantity = 0;
			foreach (var allotment in Allotments)
			{
				sum += (int)(allotment.AllotPrice * allotment.Quantity);
                quantity = (uint)(quantity + allotment.Quantity);
			}

			TotalUnits = quantity;
			TotalValue = (uint)sum;
			AveragePrice = ((uint)sum/(uint)quantity);
		}

		internal bool CanSell(uint nOrderQuantity)
		{
			return TotalUnits - nOrderQuantity >= 0;
		}

        internal void CalculateGain()
        {
			Gain = (int)((int)(LTP - AveragePrice) * TotalUnits);
        }
    }

	public class PortfolioManagerService
	{
		private HashSet<Holding> myHoldings = new HashSet<Holding>(new HoldingComparer());

		public HashSet<Holding> Holdings { get => myHoldings; set => myHoldings = value; }

		public int PL { get; set; }

		public void Buy(Order order)
		{
			var holding = new Holding()
			{
				Ticker= order.Ticker,
			};

			var allotment = new Allotment()
			{
				AllotPrice = order.Price,
				Quantity = (int)order.Quantity,
			};

			if (myHoldings.TryGetValue(holding, out var existingholding))
			{
				existingholding.Allotments.Add(allotment);
				existingholding.CalculateAverage();
			}
			else
			{
				holding.Allotments.Add(allotment);
				holding.CalculateAverage();
				myHoldings.Add(holding);
			}
		}

		public void Sell(Order order)
		{
			var holding = new Holding()
			{
				Ticker = order.Ticker,
			};

			if (myHoldings.TryGetValue(holding, out var existingholding))
			{
				if(existingholding.CanSell(order.Quantity))
				{
                    existingholding.Allotments.Add(new Allotment()
                    {
                        AllotPrice = order.Price,
                        Quantity = (int)-order.Quantity,
                    });
                    existingholding.CalculateAverage();
                }
            }
		}

		public void OnStockTickReceived(StockData stockData)
		{
			var holding = new Holding()
			{
				Ticker = stockData.TickerName,
			};

			if (myHoldings.TryGetValue(holding, out var existingholding))
			{
				existingholding.LTP = stockData.LTP;
				existingholding.CalculateGain();
			}

			PL = 0;
			foreach (var ticker in myHoldings)
			{
				PL += ticker.Gain;
			}
		}
	}
}
