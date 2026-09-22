using System.Text.Json;
using MultiLLM.Core.Interfaces;
using MultiLLM.Core.Models.Game;

namespace MultiLLM.Core.Services.Game;

public class GameService : IGameService
{
    private readonly GameEngine _gameEngine;
    private readonly GameDungeonMaster _dungeonMaster;

    public GameService(IChatCompletionServiceFactory chatServiceFactory)
    {
        var openAiService = chatServiceFactory.GetService("openai");
        _dungeonMaster = new GameDungeonMaster(openAiService);
        _gameEngine = new GameEngine(_dungeonMaster);
    }

    public async Task<Story> StartNewGameAsync(string title, string description)
    {
        return await _gameEngine.StartNewStoryAsync(title, description);
    }

    public async Task<GameActionResult> MakeChoiceAsync(string choiceId)
    {
        return await _gameEngine.ProcessChoiceAsync(choiceId);
    }

    public async Task<GameActionResult> ProcessBattleActionAsync(BattleAction action)
    {
        return _gameEngine.ProcessBattleAction(action);
    }

    public GameState GetCurrentGameState()
    {
        return _gameEngine.GetGameState();
    }

    public async Task<List<GameItem>> GenerateLootAsync(Enemy enemy)
    {
        var gameState = GetCurrentGameState();
        return await _dungeonMaster.GenerateLootAsync(enemy, gameState.Player);
    }

    public async Task SaveGameAsync(string filePath)
    {
        var gameState = GetCurrentGameState();
        var saveData = new
        {
            Story = gameState.Story,
            CurrentPageId = gameState.CurrentPage?.Id,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<Story> LoadGameAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var saveData = JsonSerializer.Deserialize<JsonElement>(json);

        // This would need proper deserialization logic
        // For now, return a new story
        return await StartNewGameAsync("Loaded Game", "A loaded adventure");
    }
}