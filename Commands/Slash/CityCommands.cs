﻿using DSharpPlus.Entities;
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
        var embed = new DiscordEmbedBuilder()
        {
            Title = "Available Buildings",
            Color = DiscordColor.Blurple,
            Description = "Here is the list of available buildings, their costs and income."
        };

        foreach (var building in _cityHandler.Buildings)
        {
            embed.AddField($"{building.Emote} {building.Name}",
                $"Cost: {building.Cost} points\nIncome: {building.Income} points", true);
        }

        await ctx.CreateResponseAsync(InteractionResponseType.UpdateMessage,
            new DiscordInteractionResponseBuilder().AddEmbed(embed));
    }


    [SlashCommand("setcityname", "Set the name of your city.")]
    public async Task SetCityNameCommand(InteractionContext ctx, [Option("name", "New name of the city")] string cityName)
    {
        bool success = await _cityHandler.SetCityName(ctx.User.Id, cityName);
        var embed = new DiscordEmbedBuilder()
        {
            Title = success ? "City Name Updated!" : "Error",
            Description = success
                ? $"🏙️ Your city's name has been set to **{cityName}**!"
                : "❌ Failed to set city name. Make sure the name is not empty.",
            Color = success ? DiscordColor.Green : DiscordColor.Red
        };
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AddEmbed(embed));
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
            var embed = new DiscordEmbedBuilder()
            {
                Title = "Error: Unknown Building",
                Description = "The building you tried to purchase does not exist. Please make sure the name is correct.",
                Color = DiscordColor.Red
            };

            await ctx.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
            return;
        }

        int buildingCost = building.Cost;

        if (userPoints < buildingCost)
        {
            var embed = new DiscordEmbedBuilder()
            {
                Title = "Error: Not Enough Points",
                Description = $"You do not have enough points to buy the {building.Name}. You need {buildingCost} points, but you only have {userPoints}.",
                Color = DiscordColor.Red
            };

            await ctx.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
            return;
        }

        var success = await _cityHandler.BuyBuilding(ctx.User.Id, building.Emote, int.Parse(x), int.Parse(y));
        var responseEmbed = new DiscordEmbedBuilder();

        if (success)
        {
            userPoints -= buildingCost;
            _pointsManager.SaveUserPoints(ctx.User.Id, userPoints);

            responseEmbed.Title = "Building Purchased Successfully";
            responseEmbed.Description = $"You have successfully purchased the {building.Name} at location ({x}, {y}).";
            responseEmbed.Color = DiscordColor.Green;
        }
        else
        {
            responseEmbed.Title = "Error: Purchase Failed";
            responseEmbed.Description = "The building could not be purchased. Make sure the location is correct and you have enough points.";
            responseEmbed.Color = DiscordColor.Red;
        }
        await ctx.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(responseEmbed));
    }



    [SlashCommand("sellbuilding", "Sell a building from a specific location.")]
    public async Task SellBuildingCommand(InteractionContext ctx,
                                         [Option("x", "x coordinate")] string x,
                                         [Option("y", "y coordinate")] string y)
    {
        var success = await _cityHandler.SellBuilding(ctx.User.Id, int.Parse(x), int.Parse(y));
        var embedBuilder = new DiscordEmbedBuilder()
        {
            Color = success ? DiscordColor.Green : DiscordColor.Red, // Używam zielonego koloru dla sukcesu i czerwonego dla błędu
            Title = success ? "Building Sold Successfully" : "Error Selling Building",
            Description = success
                ? $"You have successfully sold a building at location ({x}, {y})."
                : "The building could not be sold. Make sure there is a building at this location.",
        };
        await ctx.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(embedBuilder));
    }


    [SlashCommand("movebuilding", "Move the building to a new location.")]
    public async Task MoveBuildingCommand(InteractionContext ctx,
                                         [Option("x1", "The starting x coordinate")] string x1,
                                         [Option("y1", "The starting y coordinate")] string y1,
                                         [Option("x2", "Final x coordinate")] string x2,
                                         [Option("y2", "Final y coordinate")] string y2)
    {
        var success = await _cityHandler.MoveBuilding(ctx.User.Id, int.Parse(x1), int.Parse(y1), int.Parse(x2), int.Parse(y2));

        var embedBuilder = new DiscordEmbedBuilder()
        {
            Color = success ? DiscordColor.Green : DiscordColor.Red,
            Title = success ? "Building Moved Successfully" : "Failed to Move Building",
            Description = success
                ? $"**Building moved:**\nFrom: ({x1}, {y1})\nTo: ({x2}, {y2})"
                : "Please check if the coordinates are correct and try again.",
        };

        if (success)
        {
            embedBuilder.AddField("Coordinates Moved", $"From ({x1}, {y1}) to ({x2}, {y2})", true);
        }

        await ctx.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(embedBuilder));
    }


    [SlashCommand("collectpoints", "Collect income from buildings in your city.")]
    public async Task CollectPointsCommand(InteractionContext ctx)
    {
        var points = await _cityHandler.GetCityPoints(ctx.User.Id);
        int currentPoints = await _pointsManager.GetUserPoints(ctx.User.Id);
        int newPoints = currentPoints + points;

        _pointsManager.SaveUserPoints(ctx.User.Id, newPoints);

        var embedBuilder = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.Goldenrod, // Używam złotego koloru
            Title = "City Income Collected",
            Description = $"You have successfully collected income from your city!\n" +
                          $"**Income Collected:** {points} points\n" +
                          $"**New Balance:** {newPoints} points",
        };
        await ctx.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(embedBuilder));
    }

}
