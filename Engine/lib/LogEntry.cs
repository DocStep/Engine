namespace Engine;


public struct LogEntry {
    public LogEntry (string text, LogType type) {
        this.text = text;
        stackTrace = new System.Diagnostics.StackTrace(1);
        timestamp = Time.getCurrentTimeLog;
        this.type = type;
    }

    public string text;
    public System.Diagnostics.StackTrace stackTrace;
    public string timestamp;
    public LogType type;
}
