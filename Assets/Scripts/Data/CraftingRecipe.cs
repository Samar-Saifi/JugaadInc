using System;
using System.Collections.Generic;
using UnityEngine;

namespace JugaadInc
{
    public enum SolutionQuality
    {
        High,
        Medium,
        Low,
        Absurd
    }

    [Serializable]
    public class ItemRequirement
    {
        public ItemData SpecificItem;
        public ItemCategory RequiredCategory = ItemCategory.Material;
        public bool MustMatchSpecificItem = true;
        public int Count = 1;

        public ItemRequirement() { }

        public ItemRequirement(ItemData specificItem, int count = 1)
        {
            SpecificItem = specificItem;
            MustMatchSpecificItem = true;
            Count = count;
        }

        public ItemRequirement(ItemCategory category, int count = 1)
        {
            RequiredCategory = category;
            MustMatchSpecificItem = false;
            Count = count;
        }

        public bool Matches(ItemData itemData)
        {
            if (itemData == null) return false;
            if (MustMatchSpecificItem)
            {
                return SpecificItem != null && SpecificItem.ItemId == itemData.ItemId;
            }
            return itemData.Category == RequiredCategory;
        }
    }

    [CreateAssetMenu(fileName = "NewCraftingRecipe", menuName = "Game/Crafting Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        public string RecipeId = "recipe_id";
        public string ResultName = "Bicycle";
        public string TargetObjectiveId = "objective_build_bicycle";
        public ItemData ResultItem;
        public SolutionQuality Quality = SolutionQuality.Medium;

        public List<ItemRequirement> Ingredients = new List<ItemRequirement>();

        [Range(0, 100)]
        public int BaseEfficiencyScore = 80;
        [Range(0, 100)]
        public int BaseCreativityScore = 85;

        [TextArea(2, 4)]
        public string SuccessMessage = "Engineering principles were violated, but the bicycle works!";

        public bool Matches(List<InventoryItem> craftingItems)
        {
            if (craftingItems == null || Ingredients == null) return false;
            if (craftingItems.Count != Ingredients.Count) return false;

            List<InventoryItem> pool = new List<InventoryItem>(craftingItems);

            foreach (var req in Ingredients)
            {
                bool foundMatch = false;
                for (int i = 0; i < pool.Count; i++)
                {
                    if (req.Matches(pool[i].Data))
                    {
                        pool.RemoveAt(i);
                        foundMatch = true;
                        break;
                    }
                }
                if (!foundMatch) return false;
            }

            return pool.Count == 0;
        }

        public static CraftingRecipe CreateRuntimeInstance(
            string recipeId, string objectiveId, string resultName, SolutionQuality quality,
            int efficiency, int creativity, string message, params ItemRequirement[] requirements)
        {
            var recipe = CreateInstance<CraftingRecipe>();
            recipe.RecipeId = recipeId;
            recipe.TargetObjectiveId = objectiveId;
            recipe.ResultName = resultName;
            recipe.Quality = quality;
            recipe.BaseEfficiencyScore = efficiency;
            recipe.BaseCreativityScore = creativity;
            recipe.SuccessMessage = message;
            if (requirements != null) recipe.Ingredients.AddRange(requirements);
            return recipe;
        }
    }
}
