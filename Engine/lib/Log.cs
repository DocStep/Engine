using System.Collections.Concurrent;
using System.Diagnostics;

namespace Engine;

public enum LogType {
    log,
    warning,
    error,
    info,
    system,
}


public class Log : Singleton<Log> {

    protected override void Init () {
        Engine.Instance.de_Update += Update;

    }


    private readonly ConcurrentQueue<LogEntry> Queue = new ConcurrentQueue<LogEntry>();

    string timestamp (string timestamp) => $" (t: {timestamp})";
    public const string logSymbol = ">";
    public const string logSymbolSpace = "> ";
    

    void Update () {
        while (Queue.TryDequeue(out LogEntry entry)) {
            //string log = entry.text + entry.timestamp + Environment.NewLine + Environment.NewLine +
            string text = entry.text;
            
            switch (entry.type) {
                case LogType.log:
                case LogType.system:
                    Console.WriteLine(text);
                    break;
                case LogType.warning:
                    Console.WriteLine("? " + text);
                    break;
                case LogType.error:
                    text += Environment.NewLine + Environment.NewLine +
                        ParseLight(entry.stackTrace.ToString());
                    //Parse(entry.stackTrace.ToString());
                    ConsoleColor(entry.type);
                    Console.WriteLine("! " + $"/({ThreadUtils.currThread})" + logSymbolSpace + text);
                    break;
                default:
                    break;
            }
        }
    }


    public static void log (string text, LogType type = LogType.log) {
        if (ThreadUtils.isMainThread) {
            ConsoleColor(type);
            Console.WriteLine(logSymbolSpace + text);
        } else {
            LogEntry log = new LogEntry(text, type);
            Instance.Queue.Enqueue(log);
        }
    }
    public static void log (object? obj) {
        log(obj?.ToString() ?? string.Empty);
    }
    public static void log (params object[] args) {
        string text = string.Empty;
        for (int i = 0; i < args.Length; i++) {
            text += args[i]?.ToString() + " ";
        }
        log(text);
    }

    public static void ConsoleColor (LogType type) {
        switch (type) {
            case LogType.log:
                Console.ResetColor();
                break;
            case LogType.warning:
                Console.ForegroundColor = System.ConsoleColor.Yellow;
                break;
            case LogType.error:
                Console.ForegroundColor = System.ConsoleColor.Red;
                break;
            case LogType.info:
                Console.ForegroundColor = System.ConsoleColor.Cyan;
                break;
            case LogType.system:
                Console.ForegroundColor = System.ConsoleColor.DarkGray;
                break;
        }
    }


    static readonly string[] Ignore = {
        "System.",
        "UnityEngine.Debug",
         nameof(Log),
         nameof(log),
         typeof(Singleton<>).Name,
    };

    public static string Parse (string raw) {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        string[] lines = raw.Split('\n');
        System.Text.StringBuilder sb = new System.Text.StringBuilder(256);

        for (int i = 0; i < lines.Length; i++) {
            string line = lines[i].Trim();
            if (line.Length == 0) continue;

            /// remove "at "
            if (line.StartsWith("at "))
                line = line.Substring(3);

            /// ignore system noise
            bool skip = false;
            for (int j = 0; j < Ignore.Length; j++) {
                if (line.StartsWith(Ignore[j])) {
                    skip = true;
                    break;
                }
            }
            if (skip) continue;

            /// extract "Type.Method"
            int paren = line.IndexOf('(');
            if (paren < 0) continue;

            string call = line.Substring(0, paren);

            /// split type + method
            int lastDot = call.LastIndexOf('.');
            if (lastDot < 0) continue;

            string type = call.Substring(0, lastDot);
            string method = call.Substring(lastDot + 1);

            /// remove namespace (optional)
            int lastTypeDot = type.LastIndexOf('.');
            if (lastTypeDot >= 0)
                type = type.Substring(lastTypeDot + 1);

            /// 1. Constructor
            if (method == ".ctor") {
                method = type + "()";
            }

            /// 2. Lambda
            /// ChunkData+<>c__DisplayClass30_0.<Generate_Task>b__0
            if (type.Contains("<>c__DisplayClass")) {
                int start = method.IndexOf('<');
                int end = method.IndexOf('>');

                if (start >= 0 && end > start) {
                    string parent = method.Substring(start + 1, end - start - 1);
                    method = parent + " (lambda)";
                }

                /// fix type name (before '+')
                int plus = type.IndexOf('+');
                if (plus > 0)
                    type = type.Substring(0, plus);
            }

            /// 3. Async state machine (rare in this format but safe)
            /// <Method>d__N
            if (type.StartsWith("<")) {
                int end = type.IndexOf('>');
                if (end > 1) {
                    type = type.Substring(1, end - 1);
                }
            }

            sb.Append(type);
            sb.Append(":");
            sb.Append(method);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string ParseLight (string raw) {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        string[] lines = raw.Split('\n');
        System.Text.StringBuilder sb = new System.Text.StringBuilder(256);

        for (int i = 0; i < lines.Length; i++) {
            string line = lines[i].Trim();
            if (line.Length == 0) continue;

            /// remove "at "
            if (line.StartsWith("at "))
                line = line.Substring(3);

            /// skip noise
            bool skip = false;
            for (int j = 0; j < Ignore.Length; j++) {
                if (line.StartsWith(Ignore[j])) {
                    skip = true;
                    break;
                }
            }
            if (skip) continue;

            /// --- minimal fixes only ---

            /// fix constructor: .ctor → Type()
            int ctorIndex = line.IndexOf(".ctor");
            if (ctorIndex > 0) {
                /// find type before .ctor
                int typeStart = line.LastIndexOf('.', ctorIndex - 1);
                if (typeStart >= 0) {
                    string type = line.Substring(typeStart + 1, ctorIndex - typeStart - 1);
                    line = line.Replace(".ctor", type + "()");
                }
            }

            /// unwrap <Method> (lambda / async names), but keep rest
            int lt = line.IndexOf('<');
            int gt = line.IndexOf('>');
            if (lt >= 0 && gt > lt) {
                string inner = line.Substring(lt + 1, gt - lt - 1);
                line = line.Substring(0, lt) + inner + line.Substring(gt + 1);
            }

            sb.AppendLine(line);
        }

        return sb.ToString();
    }


    struct LogEntry {
        public LogEntry (string text, LogType type) {
            this.text = text;
            stackTrace = new StackTrace(1);
            timestamp = Time.getCurrentTimeLog;
            this.type = type;
        }

        public string text;
        public StackTrace stackTrace;
        public string timestamp;
        public LogType type;
    }

}
