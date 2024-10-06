using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace DailyWallpapers
{
    internal class Program
    {
        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        const int SW_HIDE = 5;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
        const int SPI_SETDESKWALLPAPER = 0x0014;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDCHANGE = 0x02;

        private static string baseUrl = "https://bing.com";
        private static string bingImageArchive = "https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=en-US";
        private static string outputPath = Path.Combine(Path.GetTempPath() + "dWall.jpg");

        static void Main(string[] args)
        {
            Console.Title = "Daily Wallpapers";
            Console.WriteLine($"[{DateTime.Now.ToString("MM/dd/yy hh:mm:ss tt")}] Starting app.. this window will close in 3s.\nWant to close it? Find it in the Task Manager.");
            HideWindow();
            Timer t = null; t = new(async (e) => { await RunCycle(bingImageArchive); }, null, TimeSpan.Zero, TimeSpan.FromHours(1));
            while (true) { }
        }

        static void HideWindow() { IntPtr handle = FindWindow(null, Console.Title); ShowWindow(handle, SW_HIDE); }

        static async Task RunCycle(string jsonUrl)
        {
            string jsonString = string.Empty;
            using (HttpClient client = new HttpClient()) { try { jsonString = await client.GetStringAsync(jsonUrl); } catch (Exception ex) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(ex); Console.ResetColor(); } }
            if (string.IsNullOrEmpty(jsonString)) { Console.WriteLine("Failed to fetch the wallpaper. Trying againt next cycle."); return; }

            JsonNode jsonResult = JsonNode.Parse(jsonString);
            string url = jsonResult["images"][0]["url"].ToJsonString();
            string fullUrl = ((baseUrl + url).Split(".jpg")[0] + ".jpg").Replace("\"", "");

            bool downloadSuccess = await DownloadWallpaper(fullUrl);
            if (!downloadSuccess) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"[{DateTime.Now.ToString("MM/dd/yy hh:mm:ss tt")}] Failed to download file, trying again next cycle."); Console.ResetColor(); return; }
            if (!File.Exists(outputPath)) { Console.WriteLine($"[{DateTime.Now.ToString("MM/dd/yy hh:mm:ss tt")}] Unable to locate the wallpaper file."); }

            int result = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, outputPath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            if (result > 0) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"[{DateTime.Now.ToString("MM/dd/yy hh:mm:ss tt")}] Wallpaper set."); Console.ResetColor(); }
        }

        static async Task<bool> DownloadWallpaper(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try { using (HttpResponseMessage response = await client.GetAsync(url)) { response.EnsureSuccessStatusCode(); using (Stream contentStream = await response.Content.ReadAsStreamAsync(), fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None)) { await contentStream.CopyToAsync(fileStream); } Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"[{DateTime.Now.ToString("MM/dd/yy hh:mm:ss tt")}] File downloaded successfully."); Console.ResetColor(); return true; } }
                catch (Exception ex) { Console.WriteLine($"Error downloading file: {ex.Message}"); return false; }
            }
        }
    }
}