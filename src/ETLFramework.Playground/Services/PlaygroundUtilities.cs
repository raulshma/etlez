using ETLFramework.Playground.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Reflection;

namespace ETLFramework.Playground.Services;

/// <summary>
/// Implementation of playground utilities for formatting and user interaction.
/// </summary>
public class PlaygroundUtilities : IPlaygroundUtilities
{
    private readonly ILogger<PlaygroundUtilities> _logger;

    public PlaygroundUtilities(ILogger<PlaygroundUtilities> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void DisplayHeader(string title, string? description = null)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[bold blue]{title}[/]").RuleStyle("blue"));
        
        if (!string.IsNullOrEmpty(description))
        {
            AnsiConsole.MarkupLine($"[dim]{description}[/]");
            AnsiConsole.WriteLine();
        }
    }

    /// <inheritdoc />
    public void DisplayResults<T>(IEnumerable<T> data, string title)
    {
        var dataList = data.ToList();
        if (!dataList.Any())
        {
            AnsiConsole.MarkupLine($"[yellow]No data to display for {title}[/]");
            return;
        }

        var table = new Table()
            .Title($"[bold]{title}[/]")
            .BorderColor(Color.Blue)
            .RoundedBorder();

        // Get properties for columns
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        // Add columns
        foreach (var prop in properties)
        {
            table.AddColumn(new TableColumn(prop.Name).Centered());
        }

        // Add rows (limit to first 10 for display)
        foreach (var item in dataList.Take(10))
        {
            var values = properties.Select(prop => 
            {
                var value = prop.GetValue(item);
                return value?.ToString() ?? "[dim]null[/]";
            }).ToArray();
            
            table.AddRow(values);
        }

        AnsiConsole.Write(table);

        if (dataList.Count > 10)
        {
            AnsiConsole.MarkupLine($"[dim]... and {dataList.Count - 10} more records[/]");
        }

        AnsiConsole.MarkupLine($"[green]Total records: {dataList.Count}[/]");
    }

    /// <inheritdoc />
    public string PromptForInput(string prompt, string? defaultValue = null)
    {
        var textPrompt = new TextPrompt<string>(prompt);
        
        if (!string.IsNullOrEmpty(defaultValue))
        {
            textPrompt.DefaultValue(defaultValue);
            textPrompt.ShowDefaultValue = true;
        }

        return AnsiConsole.Prompt(textPrompt);
    }

    /// <inheritdoc />
    public T PromptForSelection<T>(string prompt, IEnumerable<T> options) where T : notnull
    {
        var selectionPrompt = new SelectionPrompt<T>()
            .Title(prompt)
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more options)[/]");

        selectionPrompt.AddChoices(options);

        return AnsiConsole.Prompt(selectionPrompt);
    }

    /// <inheritdoc />
    public async Task WithProgressAsync(Func<IProgress<string>, Task> operation, string description)
    {
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask(description);
                var progress = new Progress<string>(message =>
                {
                    task.Description = message;
                    task.Increment(1);
                });

                await operation(progress);
                task.Value = 100;
            });
    }

    /// <inheritdoc />
    public void DisplayError(string message, Exception? exception = null)
    {
        AnsiConsole.MarkupLine($"[red]❌ Error: {message}[/]");
        
        if (exception != null)
        {
            _logger.LogError(exception, "Playground error: {Message}", message);
            
            if (AnsiConsole.Confirm("Show detailed error information?", false))
            {
                AnsiConsole.WriteException(exception);
            }
        }
    }

    /// <inheritdoc />
    public void DisplaySuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]✅ {message}[/]");
    }

    /// <inheritdoc />
    public void WaitForKeyPress(string? message = null)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(message ?? "[dim]Press any key to continue...[/]");
        Console.ReadKey(true);
    }
}

/// <summary>
/// Extension methods for playground utilities.
/// </summary>
public static class PlaygroundExtensions
{
    /// <summary>
    /// Converts an object to a dictionary for display purposes.
    /// </summary>
    /// <param name="obj">Object to convert</param>
    /// <returns>Dictionary representation</returns>
    public static Dictionary<string, object?> ToDictionary(this object obj)
    {
        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return properties.ToDictionary(prop => prop.Name, prop => prop.GetValue(obj));
    }

    /// <summary>
    /// Formats a timespan for display.
    /// </summary>
    /// <param name="timespan">Timespan to format</param>
    /// <returns>Formatted string</returns>
    public static string ToDisplayString(this TimeSpan timespan)
    {
        if (timespan.TotalMilliseconds < 1000)
            return $"{timespan.TotalMilliseconds:F0}ms";
        if (timespan.TotalSeconds < 60)
            return $"{timespan.TotalSeconds:F2}s";
        if (timespan.TotalMinutes < 60)
            return $"{timespan.TotalMinutes:F1}m";
        return $"{timespan.TotalHours:F1}h";
    }

    /// <summary>
    /// Formats a number for display with appropriate units.
    /// </summary>
    /// <param name="number">Number to format</param>
    /// <returns>Formatted string</returns>
    public static string ToDisplayString(this long number)
    {
        if (number < 1000)
            return number.ToString();
        if (number < 1000000)
            return $"{number / 1000.0:F1}K";
        if (number < 1000000000)
            return $"{number / 1000000.0:F1}M";
        return $"{number / 1000000000.0:F1}B";
    }
}
