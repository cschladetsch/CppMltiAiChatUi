using MultiLLM.Core.Models.Game;

namespace MultiLLM.Core.Services.Game;

public class GameEngine
{
    private readonly GameDungeonMaster _dungeonMaster;
    public Story CurrentStory { get; private set; } = new();
    public StoryPage? CurrentPage { get; private set; }

    public GameEngine(GameDungeonMaster dungeonMaster)
    {
        _dungeonMaster = dungeonMaster;
    }

    public async Task<Story> StartNewStoryAsync(string title, string description)
    {
        CurrentStory = new Story
        {
            Title = title,
            Description = description,
            Player = new Player { Name = "Adventurer" }
        };

        var startingPage = await _dungeonMaster.GeneratePageAsync(
            CurrentStory,
            "Generate the opening page of this adventure story",
            1
        );

        startingPage.Id = "start";
        CurrentStory.StartingPageId = "start";
        CurrentStory.Pages.Add(startingPage);
        CurrentPage = startingPage;

        return CurrentStory;
    }

    public async Task<GameActionResult> ProcessChoiceAsync(string choiceId)
    {
        if (CurrentPage == null)
            return new GameActionResult { Success = false, Message = "No current page" };

        var choice = CurrentPage.Choices.FirstOrDefault(c => c.Id == choiceId);
        if (choice == null)
            return new GameActionResult { Success = false, Message = "Invalid choice" };

        if (!choice.IsAvailable(CurrentStory.Player))
            return new GameActionResult { Success = false, Message = "Choice not available" };

        return await NavigateToPageAsync(choice.TargetPageId);
    }

    public async Task<GameActionResult> NavigateToPageAsync(string pageId)
    {
        var page = CurrentStory.GetPage(pageId);

        if (page == null)
        {
            // Generate new page
            page = await _dungeonMaster.GeneratePageAsync(
                CurrentStory,
                $"Continue the story from the current context",
                CurrentStory.Pages.Count + 1
            );
            page.Id = pageId;
            CurrentStory.Pages.Add(page);
        }

        CurrentPage = page;

        // Process page effects
        await ProcessPageEffectsAsync(page);

        var result = new GameActionResult
        {
            Success = true,
            Message = "Navigated to new page",
            CurrentPage = page
        };

        // Check for battle
        if (page.HasBattle && page.Battle != null)
        {
            page.Battle.StartBattle();
            result.Message = "Battle started!";
            result.BattleStarted = true;
        }

        return result;
    }

    public GameActionResult ProcessBattleAction(BattleAction action)
    {
        if (CurrentPage?.Battle == null)
            return new GameActionResult { Success = false, Message = "No active battle" };

        var battleResult = CurrentPage.Battle.ProcessPlayerAction(action, CurrentStory.Player);
        var result = new GameActionResult
        {
            Success = battleResult.Success,
            Message = battleResult.Message,
            BattleResult = battleResult
        };

        if (CurrentPage.Battle.IsComplete)
        {
            result = ProcessBattleEnd(CurrentPage.Battle, result);
        }

        return result;
    }

    private GameActionResult ProcessBattleEnd(Battle battle, GameActionResult result)
    {
        switch (battle.State)
        {
            case BattleState.Victory:
                result.Message += "\n🎉 Victory! You defeated all enemies!";

                // Give loot
                foreach (var enemy in battle.Enemies)
                {
                    foreach (var loot in enemy.LootTable)
                    {
                        if (CurrentStory.Player.Inventory.AddItem(loot))
                        {
                            result.Message += $"\n📦 You found: {loot.Name}";
                        }
                    }
                }

                // Give experience
                var expGained = battle.Enemies.Sum(e => e.MaxHealth / 5);
                CurrentStory.Player.Experience += expGained;
                result.Message += $"\n⭐ You gained {expGained} experience!";

                break;

            case BattleState.Defeat:
                result.Message += "\n💀 Defeat! You have been defeated...";
                result.GameOver = true;
                break;

            case BattleState.Fled:
                result.Message += "\n🏃 You successfully fled from battle!";
                break;
        }

        return result;
    }

    private async Task ProcessPageEffectsAsync(StoryPage page)
    {
        // Give items
        foreach (var item in page.ItemsToGive)
        {
            CurrentStory.Player.Inventory.AddItem(item);
        }

        // Apply effects
        foreach (var effect in page.EffectsToApply)
        {
            CurrentStory.Player.AddEffect(effect);
        }

        // Process active effects
        CurrentStory.Player.ProcessEffects();

        // Handle loops
        if (page.IsLoop && !string.IsNullOrEmpty(page.LoopBackToPageId))
        {
            // This could trigger a loop back to another page
            // For now, just mark it in the page variables
            page.PageVariables["loopProcessed"] = true;
        }
    }

    public GameState GetGameState()
    {
        return new GameState
        {
            Story = CurrentStory,
            CurrentPage = CurrentPage,
            Player = CurrentStory.Player,
            IsInBattle = CurrentPage?.Battle?.State == BattleState.InProgress
        };
    }
}

