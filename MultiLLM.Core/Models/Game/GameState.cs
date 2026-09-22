namespace MultiLLM.Core.Models.Game;

public class GameState
{
    public Story Story { get; set; } = new();
    public StoryPage? CurrentPage { get; set; }
    public Player Player { get; set; } = new();
    public bool IsInBattle { get; set; }
}