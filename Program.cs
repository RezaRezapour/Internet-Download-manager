using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
class Program
{
    static readonly ConcurrentQueue<string> UrlQueue = new ConcurrentQueue<string>();
    static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(0);
    static int concurrencyLevel = 3;

    static void Main(string[] args)
    {
        // List of file URLs
        List<string> urls = new List<string>
            {
                "https://dl3.tarikhema.org/music/dlfile/AgADvg0AAuGN2VM.mp3",
                "https://dl3.tarikhema.org/music/dlfile/AgADewwAAlklQVI.mp3",
                "https://dl3.tarikhema.org/music/dlfile/AgADGg0AAtcF8VI.mp3",
                "https://dl1.tarikhema.org/other/2021/07/07/CQACAgQAAxkBAAIrz1_UfWsTYGXJgmHC57kCVuKGKn5oAAIjCQACeX2oUgpRfbwJsDNfHgQ.mp3",
                "https://dl3.tarikhema.org/music/dlfile/AgADRw8AAm1Y2FM.mp3",
                "https://dl1.tarikhema.org/other/2021/07/07/CQACAgQAAxkBAAIrzF_UfWvE3Xqzbj5sOjomNyeNu28lAAIgCQACeX2oUuy920TlpOf8HgQ.mp3"
            };

        // Add URLs to the queue
        foreach (string url in urls)
        {
            UrlQueue.Enqueue(url);
        }

        // Release the semaphore to start the downloads
        Semaphore.Release(concurrencyLevel);

        // Start the initial download tasks
        for (int i = 0; i < concurrencyLevel; i++)
        {
            StartDownloadWorker();
        }

        Console.ReadLine(); // Keep the console open
    }

    static void StartDownloadWorker()
    {
        Task.Factory.StartNew(DownloadWorker);
    }

    static async void DownloadWorker()
    {
        while (true)
        {
            // Wait for a URL to become available
            await Semaphore.WaitAsync();

            // Get the next URL from the queue
            if (UrlQueue.TryDequeue(out string url))
            {
                try
                {
                    // Download the file
                    string fileName = Path.GetFileName(url);
                    string outputPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
                    await DownloadFileAsync(url, outputPath);

                    Console.WriteLine($"File '{fileName}' downloaded successfully to '{outputPath}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading file: {ex.Message}");
                }
            }
            else
            {
                // No more URLs in the queue, exit the worker
                break;
            }

            // Release the semaphore and start a new worker if there are URLs remaining
            Semaphore.Release();
            if (UrlQueue.Count > 0)
            {
                StartDownloadWorker();
            }
        }
    }

    static async Task DownloadFileAsync(string url, string outputPath)
    {
        using (WebClient client = new WebClient())
        {
            await client.DownloadFileTaskAsync(new Uri(url), outputPath);
        }
    }
}

