using System.Threading;


public class ThreadUtils {

    public static void Init () {
        mainThreadId = Thread.CurrentThread.ManagedThreadId;
        //Console.WriteLine($"{Log.logSymbolSpace}Inited: {nameof(ThreadUtils)}");
    }


    private static int mainThreadId; 
    public static bool isMainThread => Thread.CurrentThread.ManagedThreadId == mainThreadId;
    public static int currThread => Thread.CurrentThread.ManagedThreadId;

}
