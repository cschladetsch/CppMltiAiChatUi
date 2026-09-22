namespace MultiLLM.Core.Models.Game;

public class GameActionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public StoryPage? CurrentPage { get; set; }
    public BattleResult? BattleResult { get; set; }
    public bool BattleStarted { get; set; }
    public bool GameOver { get; set; }
}