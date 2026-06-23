using System;
using System.Threading;


public class ThreadUtils {

    public static void Init () {
        mainThreadId = Thread.CurrentThread.ManagedThreadId;
    }


    private static int mainThreadId; 
    public static bool isMainThread => Thread.CurrentThread.ManagedThreadId == mainThreadId;

}
