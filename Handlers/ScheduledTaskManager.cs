﻿namespace Discord_Bot.Handlers
{
    public class ScheduledTaskManager
    {
        public void ScheduleDailyTask(string taskName, List<TimeSpan> times, Func<Task> task)
        {
            foreach (var time in times)
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
                        Console.WriteLine($"Zadanie {taskName} (godzina {time}) uruchomi się za {delay}");

                        await Task.Delay(delay);
                        await task();
                    }
                });
            }
        }
    }
}