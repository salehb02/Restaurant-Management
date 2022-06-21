using UnityEngine;

public enum FoodType { Pizza, Burger, Fish, IceCream, Coffee, Sandwich }

[System.Serializable]
public class FoodsFilterEnum
{
    public FoodType FoodType;
    public Sprite foodIcon;
    public GameObject[] foodPrefabs;
}