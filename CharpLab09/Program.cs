using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private const string apiKey = "cjVMelV0TG9wTmdrOHJtNV90QzR4bEdzdExoU3RieHhVbmZZMy11dzlMND0";
    static readonly Mutex mutex = new();
    static async Task Main(string[] args)
    {
        static async Task<List<string>> ReadTickersAsync(string filePath)
        {
            return new List<string>(await File.ReadAllLinesAsync(filePath));
        }
        var tickers = await ReadTickersAsync("ticker.txt");

        List<Task> tasks = new List<Task>();
        foreach (string ticker in tickers)
        {
            tasks.Add(FetchStockDataAsync(ticker));
            System.Threading.Thread.Sleep(50);
        }

        await Task.WhenAll(tasks);

        Console.WriteLine("Средние цены акций рассчитаны и записаны в файл average_prices.txt.");






        static async Task FetchStockDataAsync(string ticker)
        {
            {
                var url = $"https://api.marketdata.app/v1/stocks/candles/D/{ticker}/?from=2024-10-11&to=2024-10-12&token={apiKey}";


                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                HttpResponseMessage response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    dynamic responseObject = Newtonsoft.Json.JsonConvert.DeserializeObject(responseContent);

                    List<long> timestamps = responseObject.t.ToObject<List<long>>();
                    List<double> highs = responseObject.h.ToObject<List<double>>();
                    List<double> lows = responseObject.l.ToObject<List<double>>();

                    double averagePrice = 0;
                    for (int i = 0; i < timestamps.Count; i++)
                    {
                        averagePrice += (highs[i] + lows[i]) / 2;
                    }
                    averagePrice /= timestamps.Count;

                    await WriteToFileAsync(ticker, averagePrice);
                }


            }


        }

        static async Task WriteToFileAsync(string ticker, double averagePrice)
        {
            mutex.WaitOne();
            try
            {
                using StreamWriter writer = new("Average.txt", true);
                await writer.WriteAsync($"{ticker}:{averagePrice} \n");
                Console.WriteLine($"{ticker}: {averagePrice}");
            }
            finally
            {
                mutex.ReleaseMutex(); // Освобождаем мьютекс после завершения операции
            }
        }
    }



}