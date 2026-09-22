namespace MultiLLM.Core.Models.Game;

public class Player
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public Inventory Inventory { get; set; } = new();
    public PlayerStats Stats { get; set; } = new();
    public List<Effect> ActiveEffects { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();

    public bool IsAlive => Health > 0;

    public void TakeDamage(int damage)
    {
        Health = Math.Max(0, Health - damage);
    }

    public void Heal(int amount)
    {
        Health = Math.Min(MaxHealth, Health + amount);
    }

    public void AddEffect(Effect effect)
    {
        ActiveEffects.Add(effect);
    }

    public void RemoveEffect(string effectId)
    {
        ActiveEffects.RemoveAll(e => e.Id == effectId);
    }

    public void ProcessEffects()
    {
        for (int i = ActiveEffects.Count - 1; i >= 0; i--)
        {
            var effect = ActiveEffects[i];
            effect.Apply(this);

            if (effect.IsExpired)
            {
                ActiveEffects.RemoveAt(i);
            }
        }
    }
}

public class PlayerStats
{
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Constitution { get; set; } = 10;
    public int Charisma { get; set; } = 10;
}