using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus;
using Discord_Bot;
using System.Text;

class CitySlashCommands : ApplicationCommandModule
{
    private readonly CityHandler _cityHandler = new CityHandler();
    private readonly VoicePointsManager _pointsManager = Program.voicePointsManager;

    [SlashCommand("buildings", "List all available buildings with their price and income.")]
    public async Task ListAllBuildingsCommand(InteractionContext ctx)
    {
        var buildingList = new StringBuilder();

        foreach (var building in _cityHandler.Buildings)
        {
            buildingList.AppendLine($"{building.Emote} {building.Name} - Cost: {building.Cost} points, Income: {building.Income} points");
        }

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent(buildingList.ToString()));
    }

    [SlashCommand("setcityname", "Set the name of your city.")]
    public async Task SetCityNameCommand(InteractionContext ctx, [Option("name", "New name of the city")] string cityName)
    {
        bool success = await _cityHandler.SetCityName(ctx.User.Id, cityName);

        if (success)
        {
            await ctx.CreateResponseAsync($"The city name has been set to: {cityName}");
        }
        else
        {
            await ctx.CreateResponseAsync("Failed to set city name. Make sure the name is not empty.");
        }
    }

    [SlashCommand("city", "View city.")]
    public async Task ViewCityCommand(InteractionContext ctx, [Option("user", "The user to check points for")] DiscordUser user = null)
    {
        ulong userId = user?.Id ?? ctx.User.Id;
        var embed = await _cityHandler.ViewCity(ctx, userId);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
    }

    [SlashCommand("buybuilding", "Buy a building on a specific site.")]
    public async Task BuyBuildingCommand(InteractionContext ctx,
                                        [Option("building", "Select a building", true)] string buildingName,
                                        [Option("x", "x coordinate")] string x,
                                        [Option("y", "y coordinate")] string y)
    {
        int userPoints = await _pointsManager.GetUserPoints(ctx.User.Id);

        var building = _cityHandler.Buildings.FirstOrDefault(b => b.Name.ToLower() == buildingName.ToLower());

        if (building == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Unknown building."));
            return;
        }

        int buildingCost = building.Cost;

        if (userPoints < buildingCost)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("You don't have enough points to buy this building."));
            return;
        }

        var success = await _cityHandler.BuyBuilding(ctx.User.Id, building.Emote, int.Parse(x), int.Parse(y));
        if (success)
        {
            userPoints -= buildingCost;
            _pointsManager.SaveUserPoints(ctx.User.Id, userPoints);

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"You successfully purchased {building.Name} at location ({x}, {y})."));
        }
        else
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("The building could not be purchased. Make sure the location is correct and you have enough points."));
        }
    }


    [SlashCommand("sellbuilding", "Sell ​​a building from a specific location.")]
    public async Task SellBuildingCommand(InteractionContext ctx,
                                         [Option("x", "x coordinate")] string x,
                                         [Option("y", "y coordinate")] string y)
    {
        var success = await _cityHandler.SellBuilding(ctx.User.Id, int.Parse(x), int.Parse(y));
        if (success)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"You have successfully sold a building at location ({x}, {y})."));
        }
        else
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("The building could not be sold. Make sure there is a building in this location."));
        }
    }

    [SlashCommand("movebuilding", "Move the building to a new location.")]
    public async Task MoveBuildingCommand(InteractionContext ctx,
                                         [Option("x1", "The starting x coordinate")] string x1,
                                         [Option("y1", "The starting y coordinate")] string y1,
                                         [Option("x2", "Final x coordinate")] string x2,
                                         [Option("y2", "Final y coordinate")] string y2)
    {
        var success = await _cityHandler.MoveBuilding(ctx.User.Id, int.Parse(x1), int.Parse(y1), int.Parse(x2), int.Parse(y2));
        if (success)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"Successfully moved building from ({x1}, {y1}) to ({x2}, {y2})."));
        }
        else
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Failed to move the building. Make sure both locations are correct."));
        }
    }

    [SlashCommand("collectpoints", "Collect income from buildings in your city.")]
    public async Task CollectPointsCommand(InteractionContext ctx)
    {
        var points = await _cityHandler.GetCityPoints(ctx.User.Id);
        int currentPoints = await _pointsManager.GetUserPoints(ctx.User.Id);
        int newPoints = currentPoints + points;

        _pointsManager.SaveUserPoints(ctx.User.Id, newPoints);

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent($"You have collected {points} points from your city's income and have them added to your account! Your new balance: {newPoints} points."));
    }

}
