using System;
using System.Collections.Generic;
using UnityEngine;

namespace JugaadInc
{
    [Serializable]
    public struct ScoreReport
    {
        public int Efficiency;
        public int Creativity;
        public int Resourcefulness;
        public int CostScore;
        public int TimeScore;
        public int FinalScore;

        public SolutionQuality Quality;
        public string OutcomeTitle;
        public string EvaluationComment;
        public string GradeString;

        public static string CalculateGrade(int score)
        {
            if (score >= 95) return "LEGENDARY (S+)";
            if (score >= 90) return "MASTER (S)";
            if (score >= 80) return "GENIUS (A)";
            if (score >= 70) return "ENGINEER (B)";
            if (score >= 60) return "PASSED (C)";
            return "APPRENTICE (D)";
        }
    }

    [CreateAssetMenu(fileName = "NewScenarioObjective", menuName = "Game/Scenario Objective")]
    public class ScenarioObjective : ScriptableObject
    {
        public string ObjectiveId = "objective_build_bicycle";
        public string Title = "Build a Bicycle";
        [TextArea(3, 5)]
        public string Description = "Scavenge the yard to find items and build a working bicycle. Why buy transportation when you can Jugaad it?";
        public float TargetTimeSeconds = 120f;
        public List<CraftingRecipe> ValidRecipes = new List<CraftingRecipe>();

        public static ScenarioObjective CreateRuntimeInstance(string id, string title, string desc, float targetTime = 120f)
        {
            var obj = CreateInstance<ScenarioObjective>();
            obj.ObjectiveId = id;
            obj.Title = title;
            obj.Description = desc;
            obj.TargetTimeSeconds = targetTime;
            return obj;
        }
    }
}
