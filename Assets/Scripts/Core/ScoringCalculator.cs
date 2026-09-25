using System.Collections.Generic;
using UnityEngine;

namespace JugaadInc
{
    public static class ScoringCalculator
    {
        public static ScoreReport CalculateScore(
            CraftingRecipe recipe,
            List<InventoryItem> materialsUsed,
            float timeElapsed,
            float targetTime = 120f)
        {
            var report = new ScoreReport();
            if (recipe == null || materialsUsed == null || materialsUsed.Count == 0)
            {
                report.FinalScore = 0;
                report.GradeString = "FAILED";
                report.EvaluationComment = "Technically, this is just a pile of objects.";
                return report;
            }

            int efficiency = recipe.BaseEfficiencyScore;
            if (materialsUsed.Count > 4) efficiency -= (materialsUsed.Count - 4) * 3;
            report.Efficiency = Mathf.Clamp(efficiency, 10, 100);

            int creativity = recipe.BaseCreativityScore;
            report.Creativity = Mathf.Clamp(creativity, 10, 100);

            float totalJunkRating = 0;
            int totalCost = 0;
            foreach (var item in materialsUsed)
            {
                if (item?.Data != null)
                {
                    totalJunkRating += item.Data.ResourcefulnessRating;
                    totalCost += item.Data.EstimatedCost;
                }
            }

            float avgJunkRating = totalJunkRating / materialsUsed.Count;
            int resourcefulness = Mathf.RoundToInt(avgJunkRating);
            report.Resourcefulness = Mathf.Clamp(resourcefulness, 15, 100);

            int costScore = Mathf.RoundToInt(100f - (totalCost * 0.6f));
            report.CostScore = Mathf.Clamp(costScore, 20, 100);

            float timeRatio = timeElapsed / Mathf.Max(10f, targetTime);
            int timeScore = 100;
            if (timeRatio <= 0.25f) timeScore = 100;
            else if (timeRatio <= 0.5f) timeScore = 92;
            else if (timeRatio <= 0.75f) timeScore = 84;
            else if (timeRatio <= 1.0f) timeScore = 75;
            else timeScore = Mathf.Max(30, 75 - Mathf.RoundToInt((timeRatio - 1.0f) * 40));
            report.TimeScore = timeScore;

            float weightedScore = (report.Efficiency * 0.20f)
                                + (report.Creativity * 0.25f)
                                + (report.Resourcefulness * 0.25f)
                                + (report.CostScore * 0.15f)
                                + (report.TimeScore * 0.15f);

            report.FinalScore = Mathf.Clamp(Mathf.RoundToInt(weightedScore), 0, 100);
            report.Quality = recipe.Quality;
            report.OutcomeTitle = recipe.ResultName;
            report.GradeString = ScoreReport.CalculateGrade(report.FinalScore);
            report.EvaluationComment = GenerateHumorousComment(recipe, report);

            return report;
        }

        private static string GenerateHumorousComment(CraftingRecipe recipe, ScoreReport report)
        {
            if (!string.IsNullOrEmpty(recipe.SuccessMessage))
            {
                return recipe.SuccessMessage;
            }

            if (report.FinalScore >= 90)
            {
                return "Not bad. You made transportation out of complete garbage!";
            }
            if (recipe.Quality == SolutionQuality.High)
            {
                return "Technically correct, but where is the Jugaad spirit? Too fancy!";
            }
            if (report.Resourcefulness >= 85)
            {
                return "A true masterpiece of scrap metal and optimism!";
            }

            return "It works! Don't push it too hard or it might disassemble itself.";
        }
    }
}
