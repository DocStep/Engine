using System.Text;
using System.Threading.Tasks;

namespace Engine;


public static class CrashHandlers {

    public static void Init (bool clean = true) {
        if (clean && Directory.Exists(folderPath)) {
            string[] filePaths = Directory.GetFiles(folderPath);
            for (int p = 0; p < filePaths.Length; p++) {
                File.Delete(filePaths[p]);
            }
        }

        /// Non-UI threads
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
            if (e.ExceptionObject is Exception ex) 
                WriteLog(ex, "AppDomain.UnhandledException");
        };

        /// Unobserved async Task exceptions
        TaskScheduler.UnobservedTaskException += (sender, e) => {
            WriteLog(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };
    }


    //private const string logname = "log";
    private static readonly string folderPath = Path.Combine(Engine.savesFolder, "logs");
    private static readonly string logDefaultFilePath = Path.Combine(folderPath, $"_default.log");
    private static string logFilePath (string source) => 
        Path.Combine(folderPath, $"crash_{(!string.IsNullOrEmpty(source) ? source : string.Empty)}.log");

    private static readonly object _lock = new object();
    public static void WriteLog (Exception ex, string source = null) {
        ConsoleColor tempColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error (Log): {source}");
        Console.ForegroundColor = tempColor;
        string log =
            "========== CRASH ==========\n" +
            $"Time: {DateTime.UtcNow:O}\n" +
            $"Source: {source}\n" +
            $"Message: {ex.Message}\n" +
            $"StackTrace:\n{ex.StackTrace}\n" +
            $"Inner:\n{ex.InnerException}\n\n";

        try {
            lock (_lock) {
                lib.DirectoryExists(folderPath);
                File.AppendAllText(logDefaultFilePath, log, Encoding.UTF8);
                File.AppendAllText(logFilePath(source), log, Encoding.UTF8);
            }
        } catch {
            /// Don't throw
            string message = $"(Logger) Error: Can't write log";
            Console.WriteLine(message);
        }
        //Environment.FailFast("Error");
    }



}
