using Newtonsoft.Json;
using RestSharp;
using ScottPlot;
using System.Diagnostics;
using System.Globalization;

namespace Candles
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // ��������� ������ � API Bybit
            var client = new RestClient("https://api.bybit.com");
            var request = new RestRequest("/v5/market/kline", Method.Get);
            request.AddParameter("symbol", "BTCUSD");
            request.AddParameter("interval", "1");
            request.AddParameter("limit", "1000");

            var response = client.Execute(request);
            var content = response.Content;

            // �������������� ������ � ������ CandlestickData
            List<OHLC> candles = new List<OHLC>();

            var marketCandles = JsonConvert.DeserializeObject<RootObject>(content);

            var marketDataList = new List<MarketData>();
            foreach (var item in marketCandles.Result.List)
            {
                marketDataList.Add(new MarketData(item));
            }

            foreach (var item in marketDataList)
            {
                var date = DateTimeOffset.FromUnixTimeMilliseconds(item.Timestamp).LocalDateTime;
                var candle = new OHLC
                {
                    TimeSpan = TimeSpan.FromMinutes(1),
                    DateTime = date,
                    Open = (double)item.OpenPrice,
                    High = (double)item.HighPrice,
                    Low = (double)item.LowPrice,
                    Close = (double)item.ClosePrice
                };
                candles.Add(candle);
            }

            // calculate SMA and display it as a scatter plot
            int[] windowSizes = { 3, 8, 20 };
            foreach (int windowSize in windowSizes)
            {
                ScottPlot.Finance.SimpleMovingAverage sma = new(candles, windowSize);
                var sp = formsPlot1.Plot.Add.Scatter(sma.Dates, sma.Means);
                sp.Label = $"SMA {windowSize}";
                sp.MarkerSize = 0;
                sp.LineWidth = 3;
                sp.Color = Colors.Navy.WithAlpha(1 - windowSize / 30.0);
            }

            // calculate Bollinger Bands
            ScottPlot.Finance.BollingerBands bb = new(candles, 20);

            // display center line (mean) as a solid line
            var sp1 = formsPlot1.Plot.Add.Scatter(bb.Dates, bb.Means);
            sp1.MarkerSize = 0;
            sp1.Color = Colors.Navy;

            // display upper bands (positive variance) as a dashed line
            var sp2 = formsPlot1.Plot.Add.Scatter(bb.Dates, bb.UpperValues);
            sp2.MarkerSize = 0;
            sp2.Color = Colors.Navy;
            sp2.LinePattern = LinePattern.Dotted;

            // display lower bands (positive variance) as a dashed line
            var sp3 = formsPlot1.Plot.Add.Scatter(bb.Dates, bb.LowerValues);
            sp3.MarkerSize = 0;
            sp3.Color = Colors.Navy;
            sp3.LinePattern = LinePattern.Dotted;

            formsPlot1.Plot.Add.Candlestick(candles);
            formsPlot1.Plot.Axes.DateTimeTicksBottom();

        }
    }

    public static class TechnicalAnalysis
    {
        public static double[] SMA(double[] values, int period)
        {
            double[] sma = new double[values.Length];

            for (int i = period - 1; i < values.Length; i++)
            {
                double sum = 0;
                for (int j = i - (period - 1); j <= i; j++)
                {
                    sum += values[j];
                }
                sma[i] = sum / period;
            }

            return sma;
        }

        public static double[] STDEV(double[] values, int period)
        {
            double[] stdev = new double[values.Length];

            for (int i = period - 1; i < values.Length; i++)
            {
                double sum = 0;
                double mean = SMA(values, period)[i];
                for (int j = i - (period - 1); j <= i; j++)
                {
                    sum += Math.Pow(values[j] - mean, 2);
                }
                stdev[i] = Math.Sqrt(sum / period);
            }

            return stdev;
        }

        public static double[] ATR(double[] close, int period)
        {
            double[] tr = new double[close.Length];
            double[] atr = new double[close.Length];

            for (int i = 1; i < close.Length; i++)
            {
                double hl = Math.Abs(close[i] - close[i - 1]);
                double hc = Math.Abs(close[i] - close[i - 1]);
                double lc = Math.Abs(close[i - 1] - close[i - 1]);

                tr[i] = Math.Max(Math.Max(hl, hc), lc);
            }

            atr[period - 1] = tr.Take(period).Average();

            for (int i = period; i < close.Length; i++)
            {
                atr[i] = (atr[i - 1] * (period - 1) + tr[i]) / period;
            }

            return atr;
        }
    }


    public class RootObject
    {
        public int RetCode { get; set; }
        public string RetMsg { get; set; }
        public Result Result { get; set; }
        public Dictionary<string, object> RetExtInfo { get; set; }
        public long Time { get; set; }
    }

    public class Result
    {
        public string Symbol { get; set; }
        public string Category { get; set; }
        public List<List<object>> List { get; set; }
    }

    // Assuming the array structure is consistent and each item has 7 elements
    public class MarketData
    {
        public long Timestamp { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal HighPrice { get; set; }
        public decimal LowPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal Volume { get; set; }

        public MarketData(List<object> data)
        {
            if (data != null && data.Count >= 7)
            {
                var culture = new CultureInfo("en-US");
                Timestamp = Convert.ToInt64(data[0], culture);
                OpenPrice = Convert.ToDecimal(data[1], culture);
                HighPrice = Convert.ToDecimal(data[2], culture);
                LowPrice = Convert.ToDecimal(data[3], culture);
                ClosePrice = Convert.ToDecimal(data[4], culture);
                Volume = Convert.ToDecimal(data[5], culture);
            }
        }
    }
}