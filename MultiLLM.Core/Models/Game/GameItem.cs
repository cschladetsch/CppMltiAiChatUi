namespace MultiLLM.Core.Models.Game;

public class GameItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItemType Type { get; set; }
    public int Quantity { get; set; } = 1;
    public Dictionary<string, object> Properties { get; set; } = new();
}

public enum ItemType
{
    Weapon,
    Armor,
    Consumable,
    Tool,
    Quest,
    Misc
}