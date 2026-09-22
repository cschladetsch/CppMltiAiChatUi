using MultiLLM.Core.Models.Game;

namespace MultiLLM.Core.Interfaces;

public interface IGameService
{
    Task<Story> StartNewGameAsync(string title, string description);
    Task<GameActionResult> MakeChoiceAsync(string choiceId);
    Task<GameActionResult> ProcessBattleActionAsync(BattleAction action);
    GameState GetCurrentGameState();
    Task<List<GameItem>> GenerateLootAsync(Enemy enemy);
    Task SaveGameAsync(string filePath);
    Task<Story> LoadGameAsync(string filePath);
}