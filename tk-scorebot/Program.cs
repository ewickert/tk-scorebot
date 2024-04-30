using System.Text;
using CommandDotNet;
using CommandDotNet.Spectre;
using discordwebhooksnet;
using Spectre.Console;
using TriviaKingsNet.Scores;

namespace tkscorebot;

public class Program
{
    public static int Main(string[] args)
    {
        return new AppRunner<Program>()
            .UseSpectreAnsiConsole().Run(args);
    }

    [DefaultCommand]
    [Command("scores", Description = "Get scores from a trivia venue.")]
    public async Task<int> GetScoresAsync(
        IAnsiConsole stdout,
        string venue,
        string url)
    {
        var lastWednesday = FindLastWednesday(DateTimeOffset.Now);
        var dl            = new ScoreDownloader(venue, lastWednesday);
        var doc           = await dl.Download();
        var parser        = new ScoreParser(doc);
        var sheet         = parser.Parse();
        var maxlen        = sheet.Select(x => x.TeamName.Length).Max();
        StringBuilder msg = new();
        msg.AppendLine("```");
        msg.AppendLine(lastWednesday.ToString());
        var header = "Team".PadRight(maxlen)
            + "|" + "1 |2 |3 |6 |9 |4 |5 |3 |6 |9 |7 |8 |3 |6 |9 |10 |Total";
        msg.AppendLine(header);
        msg.AppendLine(string.Empty.PadLeft(header.Length, '-'));
        foreach (var row in sheet)
        {
            msg.Append(row.TeamName.PadRight(maxlen));
            msg.Append('|');
            var p1 = row.Periods[0];
            for (int j = 0; j < 3; j++)
            for (int i = 0; i < 5; i++)
            {
                msg.Append(row.Periods[j][i].ToString().PadRight(2));
                msg.Append(' ');
            }
            msg.Append(row.FinalWager.ToString().PadRight(3));
            msg.Append(' ');
            msg.Append(row.Total);
            msg.AppendLine();
        }
        msg.AppendLine("```");
        //stdout.WriteLine(msg.ToString());
        var discord = new DiscordClient("scorebot", url);
        discord.SendMessageAsync(msg.ToString());
        await Task.Delay(1000);
        return 0;
    }
    static DateTimeOffset FindLastWednesday(DateTimeOffset MaybeWednesday)
    {
        while (MaybeWednesday.DayOfWeek != DayOfWeek.Wednesday)
        {
            MaybeWednesday = MaybeWednesday.AddDays(-1);
        }

        return MaybeWednesday;
    }
}