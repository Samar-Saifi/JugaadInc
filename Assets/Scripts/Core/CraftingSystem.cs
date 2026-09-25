using System;
using System.Collections.Generic;
using UnityEngine;

namespace JugaadInc
{
    public class CraftingResult
    {
        public bool Success;
        public CraftingRecipe MatchedRecipe;
        public ItemData ResultItem;
        public ScoreReport ScoreReport;
        public string FeedbackMessage;
        public SolutionQuality Quality;

        public static CraftingResult CreateFailure(string feedback)
        {
            return new CraftingResult
            {
                Success = false,
                FeedbackMessage = feedback,
                Quality = SolutionQuality.Low
            };
        }
    }

    public class CraftingSystem : MonoBehaviour
    {
        [SerializeField] private int maxCraftingSlots = 5;

        private readonly List<InventoryItem> craftingSlots = new List<InventoryItem>();
        private readonly List<CraftingRecipe> registeredRecipes = new List<CraftingRecipe>();

        private static readonly string[] GenericFailureQuotes = new string[]
        {
            "This isn't engineering. This is a cry for help.",
            "Congratulations. You have invented nothing.",
            "Technically, this is a pile of objects.",
            "Maybe add something that actually makes sense.",
            "Physics and common sense have both left the chat."
        };

        public event Action<InventoryItem> OnItemAddedToCrafting;
        public event Action<InventoryItem> OnItemRemovedFromCrafting;
        public event Action OnCraftingAreaChanged;
        public event Action<CraftingResult> OnCraftAttempted;

        public IReadOnlyList<InventoryItem> CraftingSlots => craftingSlots.AsReadOnly();
        public int SlotCount => craftingSlots.Count;
        public int MaxSlots => maxCraftingSlots;

        public void RegisterRecipe(CraftingRecipe recipe)
        {
            if (recipe != null && !registeredRecipes.Contains(recipe))
            {
                registeredRecipes.Add(recipe);
            }
        }

        public void RegisterRecipes(IEnumerable<CraftingRecipe> recipes)
        {
            if (recipes == null) return;
            foreach (var r in recipes) RegisterRecipe(r);
        }

        public bool AddToCrafting(InventoryItem item)
        {
            if (item == null || craftingSlots.Count >= maxCraftingSlots) return false;
            if (craftingSlots.Contains(item)) return false;

            craftingSlots.Add(item);
            OnItemAddedToCrafting?.Invoke(item);
            OnCraftingAreaChanged?.Invoke();
            return true;
        }

        public bool RemoveFromCrafting(InventoryItem item)
        {
            if (item == null || !craftingSlots.Contains(item)) return false;

            craftingSlots.Remove(item);
            OnItemRemovedFromCrafting?.Invoke(item);
            OnCraftingAreaChanged?.Invoke();
            return true;
        }

        public void ClearCraftingArea()
        {
            craftingSlots.Clear();
            OnCraftingAreaChanged?.Invoke();
        }

        public CraftingResult Craft(ScenarioObjective activeObjective, float timeElapsed)
        {
            if (craftingSlots.Count == 0)
            {
                var emptyResult = CraftingResult.CreateFailure("Put some items into the crafting area first!");
                OnCraftAttempted?.Invoke(emptyResult);
                return emptyResult;
            }

            List<CraftingRecipe> pool = new List<CraftingRecipe>(registeredRecipes);
            if (activeObjective != null && activeObjective.ValidRecipes != null)
            {
                foreach (var r in activeObjective.ValidRecipes)
                {
                    if (r != null && !pool.Contains(r)) pool.Add(r);
                }
            }

            CraftingRecipe matchedRecipe = null;
            foreach (var recipe in pool)
            {
                if (recipe != null && recipe.Matches(craftingSlots))
                {
                    matchedRecipe = recipe;
                    break;
                }
            }

            CraftingResult result;

            if (matchedRecipe != null)
            {
                var scoreReport = ScoringCalculator.CalculateScore(
                    matchedRecipe,
                    craftingSlots,
                    timeElapsed,
                    activeObjective != null ? activeObjective.TargetTimeSeconds : 120f
                );

                result = new CraftingResult
                {
                    Success = true,
                    MatchedRecipe = matchedRecipe,
                    ResultItem = matchedRecipe.ResultItem,
                    ScoreReport = scoreReport,
                    Quality = matchedRecipe.Quality,
                    FeedbackMessage = scoreReport.EvaluationComment
                };
            }
            else
            {
                string failureFeedback = GenerateContextualFailureMessage(craftingSlots);
                result = CraftingResult.CreateFailure(failureFeedback);
            }

            OnCraftAttempted?.Invoke(result);
            return result;
        }

        private string GenerateContextualFailureMessage(List<InventoryItem> items)
        {
            if (items.Count == 1)
            {
                return $"A single {items[0].Data.DisplayName} is not enough. You must combine multiple items!";
            }

            bool hasWheel = items.Exists(x => x.Data.ItemId.Contains("wheel"));
            bool hasBucket = items.Exists(x => x.Data.ItemId.Contains("bucket"));
            bool hasKeyboard = items.Exists(x => x.Data.ItemId.Contains("keyboard"));
            bool hasWood = items.Exists(x => x.Data.ItemId.Contains("wood"));

            if (hasBucket && hasKeyboard)
            {
                return "A bucket and a keyboard do not make a motorcycle.";
            }

            if (hasBucket && hasWheel)
            {
                return "You've created a bucket with wheels. It's technically transportation, but not very useful.";
            }

            if (hasWood && hasWheel && items.Count == 2)
            {
                return "Good start! But wood and a wheel alone will collapse under your weight. Need a fastener or frame!";
            }

            int randomIndex = UnityEngine.Random.Range(0, GenericFailureQuotes.Length);
            return GenericFailureQuotes[randomIndex];
        }
    }
}
