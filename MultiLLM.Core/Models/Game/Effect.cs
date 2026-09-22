namespace MultiLLM.Core.Models.Game;

public abstract class Effect
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Duration { get; set; }
    public int RemainingDuration { get; set; }
    public EffectType Type { get; set; }

    public bool IsExpired => RemainingDuration <= 0;

    public virtual void Apply(Player player)
    {
        RemainingDuration--;
    }
}

public class HealthEffect : Effect
{
    public int HealthChange { get; set; }

    public override void Apply(Player player)
    {
        if (HealthChange > 0)
            player.Heal(HealthChange);
        else
            player.TakeDamage(-HealthChange);

        base.Apply(player);
    }
}

public class StatEffect : Effect
{
    public string StatName { get; set; } = string.Empty;
    public int StatModifier { get; set; }

    public override void Apply(Player player)
    {
        base.Apply(player);
    }
}

public enum EffectType
{
    Buff,
    Debuff,
    Poison,
    Healing,
    Shield,
    Other
}