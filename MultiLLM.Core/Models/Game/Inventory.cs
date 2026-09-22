namespace MultiLLM.Core.Models.Game;

public class Inventory
{
    private readonly GameItem?[,] _slots = new GameItem?[3, 3];

    public GameItem?[,] Slots => _slots;
    public int Width => 3;
    public int Height => 3;

    public bool AddItem(GameItem item, int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return false;

        if (_slots[x, y] != null)
        {
            if (_slots[x, y]!.Id == item.Id && _slots[x, y]!.Type == ItemType.Consumable)
            {
                _slots[x, y]!.Quantity += item.Quantity;
                return true;
            }
            return false;
        }

        _slots[x, y] = item;
        return true;
    }

    public bool AddItem(GameItem item)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (AddItem(item, x, y))
                    return true;
            }
        }
        return false;
    }

    public GameItem? RemoveItem(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null;

        var item = _slots[x, y];
        _slots[x, y] = null;
        return item;
    }

    public GameItem? GetItem(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null;

        return _slots[x, y];
    }

    public List<GameItem> GetAllItems()
    {
        var items = new List<GameItem>();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_slots[x, y] != null)
                    items.Add(_slots[x, y]!);
            }
        }
        return items;
    }

    public bool HasSpace()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_slots[x, y] == null)
                    return true;
            }
        }
        return false;
    }
}