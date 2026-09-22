namespace MultiLLM.Core.Models.Game;

public class Story
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<StoryPage> Pages { get; set; } = new();
    public string StartingPageId { get; set; } = string.Empty;
    public Player Player { get; set; } = new();
    public Dictionary<string, object> GlobalVariables { get; set; } = new();

    public StoryPage? GetPage(string pageId)
    {
        return Pages.FirstOrDefault(p => p.Id == pageId);
    }

    public StoryPage? GetStartingPage()
    {
        return GetPage(StartingPageId);
    }
}

public class StoryPage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<Choice> Choices { get; set; } = new();
    public Battle? Battle { get; set; }
    public List<GameItem> ItemsToGive { get; set; } = new();
    public List<Effect> EffectsToApply { get; set; } = new();
    public bool IsLoop { get; set; }
    public string? LoopBackToPageId { get; set; }
    public Dictionary<string, object> PageVariables { get; set; } = new();

    public bool HasBattle => Battle != null;
    public bool HasChoices => Choices.Count > 0;
}

public class Choice
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = string.Empty;
    public string TargetPageId { get; set; } = string.Empty;
    public List<Requirement> Requirements { get; set; } = new();
    public bool IsAvailable(Player player) => Requirements.All(r => r.IsMet(player));
}

public abstract class Requirement
{
    public abstract bool IsMet(Player player);
}

public class ItemRequirement : Requirement
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;

    public override bool IsMet(Player player)
    {
        var items = player.Inventory.GetAllItems().Where(i => i.Id == ItemId);
        return items.Sum(i => i.Quantity) >= Quantity;
    }
}

public class StatRequirement : Requirement
{
    public string StatName { get; set; } = string.Empty;
    public int MinValue { get; set; }

    public override bool IsMet(Player player)
    {
        return StatName.ToLower() switch
        {
            "strength" => player.Stats.Strength >= MinValue,
            "dexterity" => player.Stats.Dexterity >= MinValue,
            "intelligence" => player.Stats.Intelligence >= MinValue,
            "constitution" => player.Stats.Constitution >= MinValue,
            "charisma" => player.Stats.Charisma >= MinValue,
            "health" => player.Health >= MinValue,
            "level" => player.Level >= MinValue,
            _ => false
        };
    }
}