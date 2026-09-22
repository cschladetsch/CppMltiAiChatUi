namespace MultiLLM.Core.Models.Game;

public class Battle
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public List<Enemy> Enemies { get; set; } = new();
    public BattleState State { get; set; } = BattleState.NotStarted;
    public int CurrentTurn { get; set; } = 0;
    public List<BattleAction> ActionHistory { get; set; } = new();

    public bool IsPlayerTurn => CurrentTurn % (Enemies.Count + 1) == 0;
    public bool IsComplete => State == BattleState.Victory || State == BattleState.Defeat;
    public List<Enemy> AliveEnemies => Enemies.Where(e => e.IsAlive).ToList();

    public void StartBattle()
    {
        State = BattleState.InProgress;
        CurrentTurn = 0;
    }

    public BattleResult ProcessPlayerAction(BattleAction action, Player player)
    {
        var result = new BattleResult();

        switch (action.Type)
        {
            case BattleActionType.Attack:
                result = ProcessAttack(action, player);
                break;
            case BattleActionType.UseItem:
                result = ProcessUseItem(action, player);
                break;
            case BattleActionType.Flee:
                result = ProcessFlee(player);
                break;
        }

        ActionHistory.Add(action);

        if (!IsComplete)
        {
            ProcessEnemyTurns(player);
            CurrentTurn++;
        }

        UpdateBattleState(player);
        return result;
    }

    private BattleResult ProcessAttack(BattleAction action, Player player)
    {
        var target = Enemies.FirstOrDefault(e => e.Id == action.TargetId);
        if (target == null || !target.IsAlive)
            return new BattleResult { Success = false, Message = "Invalid target" };

        var damage = CalculateDamage(player, target);
        target.TakeDamage(damage);

        return new BattleResult
        {
            Success = true,
            Message = $"You deal {damage} damage to {target.Name}",
            DamageDealt = damage
        };
    }

    private BattleResult ProcessUseItem(BattleAction action, Player player)
    {
        var item = player.Inventory.GetAllItems().FirstOrDefault(i => i.Id == action.ItemId);
        if (item == null)
            return new BattleResult { Success = false, Message = "Item not found" };

        if (item.Type == ItemType.Consumable && item.Properties.ContainsKey("healing"))
        {
            var healing = Convert.ToInt32(item.Properties["healing"]);
            player.Heal(healing);
            item.Quantity--;

            if (item.Quantity <= 0)
            {
                // Remove item from inventory
            }

            return new BattleResult
            {
                Success = true,
                Message = $"You use {item.Name} and heal {healing} health",
                HealthRestored = healing
            };
        }

        return new BattleResult { Success = false, Message = "Item cannot be used in battle" };
    }

    private BattleResult ProcessFlee(Player player)
    {
        var fleeChance = CalculateFleeChance(player);
        var random = new Random();

        if (random.NextDouble() < fleeChance)
        {
            State = BattleState.Fled;
            return new BattleResult { Success = true, Message = "You successfully flee from battle!" };
        }

        return new BattleResult { Success = false, Message = "You failed to flee!" };
    }

    private void ProcessEnemyTurns(Player player)
    {
        foreach (var enemy in AliveEnemies)
        {
            var damage = CalculateEnemyDamage(enemy, player);
            player.TakeDamage(damage);
            ActionHistory.Add(new BattleAction
            {
                Type = BattleActionType.Attack,
                ActorId = enemy.Id,
                Message = $"{enemy.Name} attacks for {damage} damage"
            });
        }
    }

    private void UpdateBattleState(Player player)
    {
        if (!player.IsAlive)
        {
            State = BattleState.Defeat;
        }
        else if (AliveEnemies.Count == 0)
        {
            State = BattleState.Victory;
        }
    }

    private int CalculateDamage(Player player, Enemy enemy)
    {
        var baseDamage = player.Stats.Strength + (player.Level * 2);
        var random = new Random();
        return Math.Max(1, baseDamage + random.Next(-3, 4));
    }

    private int CalculateEnemyDamage(Enemy enemy, Player player)
    {
        var random = new Random();
        return Math.Max(1, enemy.Attack + random.Next(-2, 3));
    }

    private double CalculateFleeChance(Player player)
    {
        return Math.Min(0.8, 0.3 + (player.Stats.Dexterity * 0.02));
    }
}

public class Enemy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Health { get; set; } = 50;
    public int MaxHealth { get; set; } = 50;
    public int Attack { get; set; } = 10;
    public int Defense { get; set; } = 5;
    public List<GameItem> LootTable { get; set; } = new();

    public bool IsAlive => Health > 0;

    public void TakeDamage(int damage)
    {
        Health = Math.Max(0, Health - damage);
    }
}

public class BattleAction
{
    public BattleActionType Type { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class BattleResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int DamageDealt { get; set; }
    public int HealthRestored { get; set; }
}

public enum BattleState
{
    NotStarted,
    InProgress,
    Victory,
    Defeat,
    Fled
}

public enum BattleActionType
{
    Attack,
    UseItem,
    Flee
}