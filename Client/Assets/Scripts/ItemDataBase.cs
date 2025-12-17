using UnityEngine;
using System.Collections.Generic;

public class ItemDataBase : MonoBehaviour
{
    public static ItemDataBase Instance;

    [System.Serializable]
    public struct ItemData
    {
        public int id;
        public string name;
        public Sprite icon;
    }

    public List<ItemData> itemList;
    private Dictionary<int, Sprite> itemDict = new Dictionary<int, Sprite>();

    void Awake()
    {
        Instance = this;

        foreach (var item in itemList)
        {
            if (!itemDict.ContainsKey(item.id)) itemDict.Add(item.id, item.icon);
        }
    }

    public Sprite GetItemSprite(int id)
    {
        if (itemDict.ContainsKey(id)) return itemDict[id];
        return null;
    }
}