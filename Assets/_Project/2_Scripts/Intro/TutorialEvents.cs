public static class TutorialEvents
{
    public static bool DrainStarted;
    public static bool DrainSucceeded;
    public static bool DrainFailed;

    public static void Clear()
    {
        DrainStarted = DrainSucceeded = DrainFailed = false;
    }
}