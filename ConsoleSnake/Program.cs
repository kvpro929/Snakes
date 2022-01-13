﻿using Snakes;
using Spectre.Console;
using System.Diagnostics;
using System.Drawing;

using Color = Spectre.Console.Color;

;
var originalBg = AnsiConsole.Background;
var originalFg = AnsiConsole.Foreground;
AnsiConsole.Background = Color.Black;

try
{
    var game = client.GetGrain<IGame>(Guid.Empty);

    AnsiConsole.Clear();
    AnsiConsole.WriteLine("Welcome to snekz!");
    AnsiConsole.WriteLine();

    if (await game.GetCurrentState() == GameState.InProgress)
    {
        AnsiConsole.Write("Game in progress - waiting for it to end");
    }

    while (await game.GetCurrentState() == GameState.InProgress)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    var name = AnsiConsole.Ask<string>("What's your name?");
    var self = client.GetGrain<IPlayer>(name);
    await self.SetHumanControlled(true);

    var state = await game.GetCurrentState();
    if (state == GameState.NoGame)
    {
        var playerCount = AnsiConsole.Ask<int>("How many players should there be (including NPCs)?");
        await game.InitializeNewGame(new Size(96, 24), playerCount);
        await self.JoinGame(game);
    }
    else if (state == GameState.Lobby)
    {
        await self.JoinGame(game);
    }
    else
    {
        AnsiConsole.WriteLine($"Game in unpected state {state}. Try running again.");
        return;
    }

    var expected = await game.GetExpectedPlayers();
    var boardSize = await game.GetBoardSize();

    AnsiConsole.WriteLine("Waiting for other players to join (press any key to start with NPCs for remaining)...");
    var prevCount = 0;
    while (prevCount < expected)
    {
        var currentCount = (await game.GetPlayers()).Count();
        if (currentCount != prevCount)
        {
            AnsiConsole.WriteLine($"\tNow {currentCount}/{expected} players");
            prevCount = currentCount;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(200));
        if (Console.KeyAvailable)
        {
            Console.ReadKey();
            await game.Start();
        }
    }

    AnsiConsole.WriteLine("Starting...");
    var round = 0;
    while (true)
    {
        if (await game.GetCurrentState() != GameState.InProgress || !await self.IsAlive())
        {
            break;
        }

        var canvas = new Canvas(boardSize.Width + 2, boardSize.Height + 2);
        DrawBorder(canvas);

        canvas.PixelWidth = 1;

        foreach (var b in await game.GetBerryPositions())
        {
            canvas.SetPixel(b.X + 1, b.Y + 1, Color.Red);
        }

        var players = await game.GetPlayers();
        foreach (var p in players)
        {
            if (p.GetPrimaryKeyString() == self.GetPrimaryKeyString())
            {
                await DrawPlayer(canvas, p, Color.Blue, Color.DarkBlue);
            }
            else if (await p.IsHumanControlled())
            {
                await DrawPlayer(canvas, p, Color.Orange1, Color.DarkOrange);
            }
            else
            {
                await DrawPlayer(canvas, p, Color.Green, Color.DarkGreen);
            }
        }

        AnsiConsole.Cursor.SetPosition(0, 0);
        AnsiConsole.Write(canvas);
        AnsiConsole.Cursor.SetPosition(1, boardSize.Height + 2);
        AnsiConsole.Markup($"[black on white]Score: {await self.GetScore()}[/]");
        AnsiConsole.Cursor.SetPosition(0, 0);

        var prev = round;
        while (prev == round)
        {
            round = await game.GetCurrentRound();
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.LeftArrow)
                {
                    await self.TurnLeft();
                }
                else if (key == ConsoleKey.RightArrow)
                {
                    await self.TurnRight();
                }
            }
        }
    }

    AnsiConsole.Cursor.SetPosition(5, boardSize.Height - 5);
    AnsiConsole.MarkupLine($"[bold yellow on blue]GAME OVER! Your score was: {await self.GetScore()}.  You {(await self.IsAlive() ? "won!" : "lost :'(")}[/]");
}
catch (Exception e)
{
    AnsiConsole.WriteLine($"\nException while trying to run client: {e.Message}");
    AnsiConsole.WriteLine("Make sure the silo the client is trying to connect to is running.");
    AnsiConsole.WriteLine("\nPress any key to exit.");
    Console.ReadKey();
    return;
}
finally
{
    AnsiConsole.Background = originalBg;
    AnsiConsole.Foreground = originalFg;
}

static async Task DrawPlayer(Canvas canvas, IPlayer player, Color headColor, Color tailColor)
{
    var body = await player.GetBody();
    var head = body.First();
    canvas.SetPixel(head.X + 1, head.Y + 1, headColor);

    foreach (var px in body.Skip(1))
    {
        canvas.SetPixel(px.X + 1, px.Y + 1, tailColor);
    }
}

static void DrawBorder(Canvas canvas)
{
    for (int x = 0; x < canvas.Width; x++)
    {
        canvas.SetPixel(x, 0, Color.White);
        canvas.SetPixel(x, canvas.Height - 1, Color.White);
    }

    for (int y = 1; y < canvas.Height - 1; y++)
    {
        canvas.SetPixel(0, y, Color.White);
        canvas.SetPixel(canvas.Width - 1, y, Color.White);
    }
}
