namespace Discord_Bot.Handlers
{
    public class ScheduledTaskManager
    {
        public void ScheduleDailyTask(string taskName, TimeSpan time, Func<Task> task)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    DateTime now = DateTime.Now;
                    DateTime nextRun = now.Date + time;

                    if (nextRun <= now)
                        nextRun = nextRun.AddDays(1);

                    TimeSpan delay = nextRun - now;
                    Console.WriteLine($"Zadanie {taskName} uruchomi się za {delay}");

                    await Task.Delay(delay);
                    await task();
                }
            });
        }
    }
}