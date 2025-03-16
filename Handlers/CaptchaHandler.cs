using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System.Drawing;
using System.Drawing.Imaging;

public static class CaptchaHandler
{
    private static Dictionary<ulong, (DateTime CooldownEnd, bool RequiresCaptcha, bool MustCompleteCaptcha)> captchaCooldowns = new();

    public static bool CheckCaptchaCooldown(ulong userId)
    {
        if (captchaCooldowns.TryGetValue(userId, out var cooldownData))
        {
            return cooldownData.CooldownEnd > DateTime.UtcNow;
        }
        return false;
    }

    public static TimeSpan GetRemainingCooldownTime(ulong userId)
    {
        if (captchaCooldowns.TryGetValue(userId, out var cooldownData) && cooldownData.CooldownEnd > DateTime.UtcNow)
        {
            return cooldownData.CooldownEnd - DateTime.UtcNow;
        }
        return TimeSpan.Zero;
    }

    public static void SetCaptchaCooldown(ulong userId, int cooldownDurationInSeconds, bool mustCompleteCaptcha)
    {
        DateTime cooldownEnd = DateTime.UtcNow.AddSeconds(cooldownDurationInSeconds);
        captchaCooldowns[userId] = (cooldownEnd, false, mustCompleteCaptcha);
    }

    public static bool MustCompleteCaptcha(ulong userId)
    {
        return captchaCooldowns.TryGetValue(userId, out var cooldownData) && cooldownData.MustCompleteCaptcha;
    }

    public static async Task<bool> VerifyCaptchaAnswer(InteractionContext ctx, int correctAnswer)
    {
        var interactivity = ctx.Client.GetInteractivity();
        var response = await interactivity.WaitForMessageAsync(m => m.Author.Id == ctx.User.Id, TimeSpan.FromSeconds(15));

        if (response.TimedOut || response.Result.Content != correctAnswer.ToString())
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("❌ Zła odpowiedź! Musisz poczekać 60 sekund.").AsEphemeral());
            SetCaptchaCooldown(ctx.User.Id, 60, false);
            await response.Result.DeleteAsync();
            return false;
        }

        SetCaptchaCooldown(ctx.User.Id, 0, false);
        await response.Result.DeleteAsync();
        return true;
    }

    public static string GenerateCaptchaImage(int num1, int num2)
    {
        string path = Path.Combine(Path.GetTempPath(), "captcha.png");
        using (Bitmap bitmap = new Bitmap(200, 80))
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.White);

            using (Font font = new Font("Arial", 30, FontStyle.Bold))
            {
                g.DrawString($"{num1} + {num2} = ?", font, Brushes.Black, new PointF(20, 20));
            }

            Random random = new Random();
            for (int i = 0; i < 10000; i++)
            {
                int x = random.Next(0, bitmap.Width);
                int y = random.Next(0, bitmap.Height);
                Color randomColor = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
                bitmap.SetPixel(x, y, randomColor);
            }

            bitmap.Save(path, ImageFormat.Png);
        }
        return path;
    }
}
