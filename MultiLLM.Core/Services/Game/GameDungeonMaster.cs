using MultiLLM.Core.Interfaces;
using MultiLLM.Core.Models;
using MultiLLM.Core.Models.Game;

namespace MultiLLM.Core.Services.Game;

public class GameDungeonMaster
{
    private readonly IChatCompletionService _chatService;
    private readonly Random _random = new();

    public GameDungeonMaster(IChatCompletionService chatService)
    {
        _chatService = chatService;
    }

    public async Task<StoryPage> GeneratePageAsync(Story story, string prompt, int pageNumber)
    {
        var systemPrompt = BuildSystemPrompt(story);
        var userPrompt = BuildPagePrompt(story, prompt, pageNumber);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        var model = new ModelDefinition { Name = "GPT-3.5 Turbo", ModelId = "gpt-3.5-turbo", Provider = "openai" };
        var response = await _chatService.CompleteAsync(model, messages, apiKey, CancellationToken.None);
        return ParsePageResponse(response);
    }

    public async Task<Battle> GenerateBattleAsync(Story story, string context)
    {
        var systemPrompt = BuildBattleSystemPrompt();
        var userPrompt = BuildBattlePrompt(story, context);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        var model = new ModelDefinition { Name = "GPT-3.5 Turbo", ModelId = "gpt-3.5-turbo", Provider = "openai" };
        var response = await _chatService.CompleteAsync(model, messages, apiKey, CancellationToken.None);
        return ParseBattleResponse(response);
    }

    public async Task<List<GameItem>> GenerateLootAsync(Enemy enemy, Player player)
    {
        var systemPrompt = "You are a loot generator for a choose your own adventure game. Generate appropriate loot.";
        var userPrompt = $"Generate 1-3 items as loot from defeating {enemy.Name}. Player level: {player.Level}";

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        var model = new ModelDefinition { Name = "GPT-3.5 Turbo", ModelId = "gpt-3.5-turbo", Provider = "openai" };
        var response = await _chatService.CompleteAsync(model, messages, apiKey, CancellationToken.None);
        return ParseLootResponse(response);
    }

    private string BuildSystemPrompt(Story story)
    {
        return $@"You are an AI Dungeon Master for a choose your own adventure game.

Story Context: {story.Description}
Player Level: {story.Player.Level}
Player Health: {story.Player.Health}/{story.Player.MaxHealth}

Generate engaging story pages with:
- Compelling narrative text
- 2-4 meaningful choices
- Optional battles (indicate with BATTLE: true)
- Optional items to give (use ITEMS: format)
- Optional loops (use LOOP: pageId)

Format your response as:
TITLE: [page title]
CONTENT: [narrative text]
BATTLE: [true/false]
ITEMS: [item1, item2] (optional)
LOOP: [pageId] (optional)
CHOICES:
1. [choice text] -> [targetPageId]
2. [choice text] -> [targetPageId]
...

Keep content engaging and appropriate for the story theme.";
    }

    private string BuildPagePrompt(Story story, string prompt, int pageNumber)
    {
        var inventoryItems = string.Join(", ", story.Player.Inventory.GetAllItems().Select(i => i.Name));

        return $@"Generate page {pageNumber} for the story.
Context: {prompt}

Player Status:
- Health: {story.Player.Health}/{story.Player.MaxHealth}
- Level: {story.Player.Level}
- Inventory: {(string.IsNullOrEmpty(inventoryItems) ? "Empty" : inventoryItems)}

Make this page exciting and give meaningful choices that affect the story.";
    }

    private string BuildBattleSystemPrompt()
    {
        return @"You are generating a battle encounter for a choose your own adventure game.
Create engaging enemies with appropriate stats.

Format your response as:
ENEMY: [name]
DESCRIPTION: [description]
HEALTH: [number]
ATTACK: [number]
DEFENSE: [number]
LOOT: [item1, item2] (optional)";
    }

    private string BuildBattlePrompt(Story story, string context)
    {
        return $@"Generate a battle appropriate for:
Context: {context}
Player Level: {story.Player.Level}
Player Health: {story.Player.Health}

Create a challenging but fair encounter.";
    }

    private StoryPage ParsePageResponse(string response)
    {
        var page = new StoryPage();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("TITLE:"))
                page.Title = trimmed[6..].Trim();
            else if (trimmed.StartsWith("CONTENT:"))
                page.Content = trimmed[8..].Trim();
            else if (trimmed.StartsWith("BATTLE:"))
                page.Battle = bool.TryParse(trimmed[7..].Trim(), out var hasBattle) && hasBattle ? new Battle() : null;
            else if (trimmed.StartsWith("LOOP:"))
                page.LoopBackToPageId = trimmed[5..].Trim();
            else if (trimmed.StartsWith("ITEMS:"))
                page.ItemsToGive = ParseItems(trimmed[6..].Trim());
            else if (char.IsDigit(trimmed[0]) && trimmed.Contains("->"))
                page.Choices.Add(ParseChoice(trimmed));
        }

        // Generate random battle if indicated
        if (page.Battle != null)
        {
            page.Battle = GenerateRandomBattle();
        }

        return page;
    }

    private Battle ParseBattleResponse(string response)
    {
        var battle = new Battle();
        var enemy = new Enemy();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("ENEMY:"))
                enemy.Name = trimmed[6..].Trim();
            else if (trimmed.StartsWith("DESCRIPTION:"))
                enemy.Description = trimmed[12..].Trim();
            else if (trimmed.StartsWith("HEALTH:") && int.TryParse(trimmed[7..].Trim(), out var health))
            {
                enemy.Health = health;
                enemy.MaxHealth = health;
            }
            else if (trimmed.StartsWith("ATTACK:") && int.TryParse(trimmed[7..].Trim(), out var attack))
                enemy.Attack = attack;
            else if (trimmed.StartsWith("DEFENSE:") && int.TryParse(trimmed[8..].Trim(), out var defense))
                enemy.Defense = defense;
            else if (trimmed.StartsWith("LOOT:"))
                enemy.LootTable = ParseItems(trimmed[5..].Trim());
        }

        battle.Enemies.Add(enemy);
        return battle;
    }

    private List<GameItem> ParseLootResponse(string response)
    {
        return ParseItems(response);
    }

    private List<GameItem> ParseItems(string itemsText)
    {
        var items = new List<GameItem>();
        if (string.IsNullOrWhiteSpace(itemsText)) return items;

        var itemNames = itemsText.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var itemName in itemNames)
        {
            var item = new GameItem
            {
                Id = Guid.NewGuid().ToString(),
                Name = itemName.Trim(),
                Type = DetermineItemType(itemName.Trim()),
                Quantity = 1
            };

            // Add some basic properties based on type
            switch (item.Type)
            {
                case ItemType.Consumable when item.Name.ToLower().Contains("potion"):
                    item.Properties["healing"] = _random.Next(15, 35);
                    break;
                case ItemType.Weapon:
                    item.Properties["damage"] = _random.Next(5, 15);
                    break;
            }

            items.Add(item);
        }

        return items;
    }

    private Choice ParseChoice(string choiceText)
    {
        var parts = choiceText.Split("->", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return new Choice();

        var text = parts[0].Trim();
        if (text.StartsWith(char.IsDigit(text[0]) ? "1." : ""))
            text = text[2..].Trim();

        return new Choice
        {
            Text = text,
            TargetPageId = parts[1].Trim()
        };
    }

    private ItemType DetermineItemType(string itemName)
    {
        var name = itemName.ToLower();

        if (name.Contains("sword") || name.Contains("axe") || name.Contains("bow"))
            return ItemType.Weapon;
        if (name.Contains("armor") || name.Contains("shield"))
            return ItemType.Armor;
        if (name.Contains("potion") || name.Contains("food"))
            return ItemType.Consumable;
        if (name.Contains("key") || name.Contains("map"))
            return ItemType.Quest;

        return ItemType.Misc;
    }

    private Battle GenerateRandomBattle()
    {
        var battle = new Battle();
        var enemy = new Enemy
        {
            Name = GetRandomEnemyName(),
            Health = _random.Next(20, 60),
            Attack = _random.Next(8, 16),
            Defense = _random.Next(2, 8)
        };
        enemy.MaxHealth = enemy.Health;

        battle.Enemies.Add(enemy);
        return battle;
    }

    private string GetRandomEnemyName()
    {
        var names = new[] { "Goblin", "Orc", "Wolf", "Bandit", "Skeleton", "Spider", "Rat", "Zombie" };
        return names[_random.Next(names.Length)];
    }
}