using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JugaadInc
{
    public enum GamePhase
    {
        MainMenu,
        Instructions,
        Playing,
        Paused,
        Evaluation,
        GameOver
    }

    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Systems")]
        [SerializeField] private InventorySystem inventorySystem;
        [SerializeField] private CraftingSystem craftingSystem;

        [Header("Current Level Objective")]
        [SerializeField] private ScenarioObjective activeObjective;

        public InventorySystem Inventory => inventorySystem;
        public CraftingSystem Crafting => craftingSystem;
        public GamePhase Phase { get; private set; } = GamePhase.Instructions;
        public ScenarioObjective CurrentObjective => activeObjective;
        public ScoreReport LastScoreReport { get; private set; }

        public float TimeElapsed { get; private set; }
        public int TotalPoints { get; private set; }

        private string notificationMessage;
        private float notificationTime;
        private Color notificationColor = Color.white;

        private GUIStyle titleStyle, headingStyle, bodyStyle, centeredStyle, buttonStyle, scoreValueStyle, badgeStyle;
        private Texture2D darkPanelTex, highlightPanelTex, craftingAreaTex, slotTex, goldTex;
        private Camera gameCamera;

        const string HighScoreKey = "HighScore";

        private enum DragSource { None, Inventory, Crafting }
        private InventoryItem draggedItem;
        private DragSource currentDragSource = DragSource.None;
        private Rect craftingBenchAreaRect;
        private Rect inventoryBarAreaRect;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureManagerExists()
        {
            if (FindFirstObjectByType<GameManager>() == null)
            {
                var go = new GameObject("Game Manager");
                go.AddComponent<GameManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Application.targetFrameRate = 120;

            EnsureSystems();
            gameCamera = Camera.main;
        }

        private void EnsureSystems()
        {
            if (inventorySystem == null)
            {
                inventorySystem = GetComponent<InventorySystem>() ?? gameObject.AddComponent<InventorySystem>();
            }
            if (craftingSystem == null)
            {
                craftingSystem = GetComponent<CraftingSystem>() ?? gameObject.AddComponent<CraftingSystem>();
            }
        }

        private void Start()
        {
            InitTextures();
            if (activeObjective != null)
            {
                craftingSystem.RegisterRecipes(activeObjective.ValidRecipes);
            }
        }

        private void Update()
        {
            if (notificationTime > 0)
            {
                notificationTime -= Time.unscaledDeltaTime;
            }

            if (Phase == GamePhase.Playing)
            {
                TimeElapsed += Time.deltaTime;

                if (Keyboard.current != null)
                {
                    if (Keyboard.current.escapeKey.wasPressedThisFrame)
                    {
                        PauseGame();
                    }

                    if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
                    {
                        AttemptCraft();
                    }

                    for (int i = 0; i < 9; i++)
                    {
                        Key numberKey = Key.Digit1 + i;
                        if (Keyboard.current[numberKey].wasPressedThisFrame)
                        {
                            QuickTransferInventoryToCrafting(i);
                        }
                    }
                }
            }
            else if (Phase == GamePhase.Paused)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    ResumeGame();
                }
            }
        }

        public void SetObjective(ScenarioObjective objective)
        {
            activeObjective = objective;
            if (activeObjective != null)
            {
                craftingSystem.RegisterRecipes(activeObjective.ValidRecipes);
            }
        }

        public void StartLevel()
        {
            TimeElapsed = 0f;
            inventorySystem.Clear();
            craftingSystem.ClearCraftingArea();
            Phase = GamePhase.Playing;
            Time.timeScale = 1f;
            ShowNotification(activeObjective != null ? $"OBJECTIVE: {activeObjective.Title}" : "LEVEL STARTED", new Color(1f, 0.85f, 0.2f));
        }

        public void AttemptCraft()
        {
            if (Phase != GamePhase.Playing) return;

            var result = craftingSystem.Craft(activeObjective, TimeElapsed);
            if (result.Success)
            {
                LastScoreReport = result.ScoreReport;
                TotalPoints += result.ScoreReport.FinalScore;
                Phase = GamePhase.Evaluation;
                Time.timeScale = 0f;

                int prevHigh = PlayerPrefs.GetInt(HighScoreKey, 0);
                if (TotalPoints > prevHigh)
                {
                    PlayerPrefs.SetInt(HighScoreKey, TotalPoints);
                    PlayerPrefs.Save();
                }
            }
            else
            {
                ShowNotification(result.FeedbackMessage, new Color(1f, 0.35f, 0.35f));
            }
        }

        public void QuickTransferInventoryToCrafting(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= inventorySystem.Count) return;
            var item = inventorySystem.Items[slotIndex];
            if (craftingSystem.AddToCrafting(item))
            {
                inventorySystem.RemoveItem(item);
            }
        }

        public void TransferCraftingToInventory(InventoryItem item)
        {
            if (item == null) return;
            if (inventorySystem.AddItem(item.Data, item.Quantity))
            {
                craftingSystem.RemoveFromCrafting(item);
            }
        }

        public void ShowNotification(string message, Color? color = null)
        {
            notificationMessage = message;
            notificationTime = 3.5f;
            notificationColor = color ?? Color.white;
        }

        public void PlayItemCollectFx(Vector3 position, Color color)
        {
            var go = new GameObject("CollectFx");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 2.5f;
            main.startSize = 0.25f;
            main.startColor = color;
            main.maxParticles = 20;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 15) });
            Destroy(go, 0.6f);
        }

        private void PauseGame()
        {
            Phase = GamePhase.Paused;
            Time.timeScale = 0f;
        }

        private void ResumeGame()
        {
            Phase = GamePhase.Playing;
            Time.timeScale = 1f;
        }

        private void InitTextures()
        {
            darkPanelTex = MakeTex(1, 1, new Color(0.04f, 0.05f, 0.08f, 0.94f));
            highlightPanelTex = MakeTex(1, 1, new Color(0.12f, 0.16f, 0.24f, 0.95f));
            craftingAreaTex = MakeTex(1, 1, new Color(0.18f, 0.14f, 0.10f, 0.90f));
            slotTex = MakeTex(1, 1, new Color(0.10f, 0.12f, 0.16f, 0.85f));
            goldTex = MakeTex(1, 1, new Color(0.95f, 0.78f, 0.18f));
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void InitStyles()
        {
            if (bodyStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.98f, 0.82f, 0.22f) } };
            headingStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.92f, 0.94f, 0.98f) } };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, normal = { textColor = new Color(0.85f, 0.88f, 0.92f) } };
            centeredStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter };
            scoreValueStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(0.35f, 0.95f, 0.55f) } };
            badgeStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.88f, 0.25f) } };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold, fixedHeight = 44 };
        }

        private void OnGUI()
        {
            InitStyles();

            if (Phase == GamePhase.Instructions)
            {
                DrawInstructionsModal();
                return;
            }

            DrawTopObjectiveHeader();
            DrawCraftingInterface();
            DrawInventoryBar();
            DrawNotificationBanner();

            if (Phase == GamePhase.Evaluation)
            {
                DrawEvaluationModal();
            }
            else if (Phase == GamePhase.Paused)
            {
                DrawPauseModal();
            }
        }

        private void DrawTopObjectiveHeader()
        {
            GUI.Box(new Rect(16, 14, 380, 85), GUIContent.none);
            GUI.Label(new Rect(28, 18, 360, 26), "OBJECTIVE: " + (activeObjective != null ? activeObjective.Title.ToUpper() : "NONE"), headingStyle);
            GUI.Label(new Rect(28, 44, 360, 48), activeObjective != null ? activeObjective.Description : "", bodyStyle);

            GUI.Box(new Rect(Screen.width - 240, 14, 225, 85), GUIContent.none);
            int seconds = Mathf.FloorToInt(TimeElapsed);
            GUI.Label(new Rect(Screen.width - 225, 20, 200, 26), $"TIME: {seconds / 60:00}:{seconds % 60:00}", headingStyle);
            GUI.Label(new Rect(Screen.width - 225, 48, 200, 26), $"SCORE: {TotalPoints}", scoreValueStyle);
        }

        private void DrawInventoryBar()
        {
            float panelWidth = Mathf.Min(780, Screen.width - 32);
            float panelHeight = 135;
            float startX = (Screen.width - panelWidth) / 2f;
            float startY = Screen.height - panelHeight - 16;
            inventoryBarAreaRect = new Rect(startX, startY, panelWidth, panelHeight);

            GUI.Box(inventoryBarAreaRect, GUIContent.none);
            GUI.Label(new Rect(startX + 16, startY + 8, panelWidth - 32, 24), $"INVENTORY ({inventorySystem.Count}/{inventorySystem.MaxCapacity}) — Drag or Click item to Craft", headingStyle);

            float slotSize = 64;
            float padding = 10;
            int slotsPerRow = Mathf.FloorToInt((panelWidth - 32) / (slotSize + padding));
            float itemsStartX = startX + 16;
            float itemsStartY = startY + 38;

            Event evt = Event.current;
            Vector2 mousePos = evt.mousePosition;

            for (int i = 0; i < inventorySystem.MaxCapacity && i < slotsPerRow; i++)
            {
                Rect slotRect = new Rect(itemsStartX + i * (slotSize + padding), itemsStartY, slotSize, slotSize);
                bool hasItem = i < inventorySystem.Count;
                var item = hasItem ? inventorySystem.Items[i] : null;

                if (hasItem && item != null)
                {
                    GUI.color = item.Data.DisplayColor;
                    GUI.DrawTexture(slotRect, slotTex);
                    GUI.color = Color.white;
                    GUI.Box(slotRect, GUIContent.none);

                    if (evt.type == EventType.MouseDown && slotRect.Contains(mousePos))
                    {
                        draggedItem = item;
                        currentDragSource = DragSource.Inventory;
                        evt.Use();
                    }

                    if (GUI.Button(slotRect, new GUIContent("", item.Data.DisplayName)))
                    {
                        QuickTransferInventoryToCrafting(i);
                    }

                    string shortName = item.Data.DisplayName.Length > 8 ? item.Data.DisplayName.Substring(0, 7) + ".." : item.Data.DisplayName;
                    GUI.Label(new Rect(slotRect.x, slotRect.y + slotSize - 22, slotSize, 20), shortName, centeredStyle);
                }
                else
                {
                    GUI.color = new Color(0.3f, 0.3f, 0.35f, 0.4f);
                    GUI.DrawTexture(slotRect, slotTex);
                    GUI.color = Color.white;
                    GUI.Box(slotRect, GUIContent.none);
                }
            }

            HandleMouseDrop(evt, mousePos);
        }

        private void DrawCraftingInterface()
        {
            float width = 480;
            float height = 180;
            float startX = (Screen.width - width) / 2f;
            float startY = Screen.height - 350;
            craftingBenchAreaRect = new Rect(startX, startY, width, height);

            GUI.Box(craftingBenchAreaRect, GUIContent.none);
            GUI.Label(new Rect(startX, startY + 10, width, 24), "CRAFTING BENCH — Combine items & press Craft", centeredStyle);

            float slotSize = 64;
            float padding = 12;
            int maxSlots = craftingSystem.MaxSlots;
            float totalSlotsWidth = maxSlots * slotSize + (maxSlots - 1) * padding;
            float slotsStartX = startX + (width - totalSlotsWidth) / 2f;
            float slotsStartY = startY + 45;

            Event evt = Event.current;
            Vector2 mousePos = evt.mousePosition;

            for (int i = 0; i < maxSlots; i++)
            {
                Rect slotRect = new Rect(slotsStartX + i * (slotSize + padding), slotsStartY, slotSize, slotSize);
                bool hasItem = i < craftingSystem.CraftingSlots.Count;
                var item = hasItem ? craftingSystem.CraftingSlots[i] : null;

                if (hasItem && item != null)
                {
                    GUI.color = item.Data.DisplayColor;
                    GUI.DrawTexture(slotRect, highlightPanelTex);
                    GUI.color = Color.white;
                    GUI.Box(slotRect, GUIContent.none);

                    if (evt.type == EventType.MouseDown && slotRect.Contains(mousePos))
                    {
                        draggedItem = item;
                        currentDragSource = DragSource.Crafting;
                        evt.Use();
                    }

                    if (GUI.Button(slotRect, new GUIContent("", "Click to remove back to Inventory")))
                    {
                        TransferCraftingToInventory(item);
                    }

                    string shortName = item.Data.DisplayName.Length > 8 ? item.Data.DisplayName.Substring(0, 7) + ".." : item.Data.DisplayName;
                    GUI.Label(new Rect(slotRect.x, slotRect.y + slotSize - 22, slotSize, 20), shortName, centeredStyle);
                }
                else
                {
                    GUI.color = new Color(0.25f, 0.22f, 0.18f, 0.5f);
                    GUI.DrawTexture(slotRect, craftingAreaTex);
                    GUI.color = Color.white;
                    GUI.Box(slotRect, GUIContent.none);
                    GUI.Label(slotRect, "+", centeredStyle);
                }
            }

            if (GUI.Button(new Rect(startX + 120, startY + 122, 240, 44), "CRAFT!", buttonStyle))
            {
                AttemptCraft();
            }

            if (draggedItem != null && evt.type == EventType.Repaint)
            {
                Rect dragPreviewRect = new Rect(mousePos.x - 30, mousePos.y - 30, 60, 60);
                GUI.color = draggedItem.Data.DisplayColor;
                GUI.DrawTexture(dragPreviewRect, highlightPanelTex);
                GUI.color = Color.white;
                GUI.Box(dragPreviewRect, GUIContent.none);
                GUI.Label(dragPreviewRect, draggedItem.Data.DisplayName, centeredStyle);
            }
        }

        private void HandleMouseDrop(Event evt, Vector2 mousePos)
        {
            if (evt.type == EventType.MouseUp && draggedItem != null)
            {
                if (currentDragSource == DragSource.Inventory && craftingBenchAreaRect.Contains(mousePos))
                {
                    if (craftingSystem.AddToCrafting(draggedItem))
                    {
                        inventorySystem.RemoveItem(draggedItem);
                        ShowNotification($"Placed {draggedItem.Data.DisplayName} on Crafting Bench", new Color(0.4f, 1f, 0.5f));
                    }
                }
                else if (currentDragSource == DragSource.Crafting && (inventoryBarAreaRect.Contains(mousePos) || !craftingBenchAreaRect.Contains(mousePos)))
                {
                    TransferCraftingToInventory(draggedItem);
                    ShowNotification($"Returned {draggedItem.Data.DisplayName} to Inventory", new Color(0.9f, 0.9f, 0.4f));
                }

                draggedItem = null;
                currentDragSource = DragSource.None;
                evt.Use();
            }
        }

        private void DrawNotificationBanner()
        {
            if (notificationTime <= 0 || string.IsNullOrEmpty(notificationMessage)) return;

            float w = Mathf.Min(600, Screen.width - 40);
            float h = 42;
            Rect rect = new Rect((Screen.width - w) / 2f, 110, w, h);

            GUI.color = new Color(0.05f, 0.06f, 0.09f, 0.92f);
            GUI.DrawTexture(rect, darkPanelTex);
            GUI.color = notificationColor;
            GUI.Box(rect, GUIContent.none);
            GUI.Label(rect, notificationMessage, centeredStyle);
            GUI.color = Color.white;
        }

        private void DrawEvaluationModal()
        {
            GUI.color = new Color(0, 0, 0, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkPanelTex);
            GUI.color = Color.white;

            float w = Mathf.Min(640, Screen.width - 32);
            float h = 500;
            Rect r = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

            GUI.Box(r, GUIContent.none);
            GUI.color = new Color(1f, 0.78f, 0.2f);
            GUI.DrawTexture(new Rect(r.x, r.y, w, 6), goldTex);
            GUI.color = Color.white;

            GUI.Label(new Rect(r.x + 20, r.y + 20, w - 40, 40), "CRAFT COMPLETE!", titleStyle);
            GUI.Label(new Rect(r.x + 20, r.y + 65, w - 40, 30), $"{LastScoreReport.OutcomeTitle.ToUpper()}", badgeStyle);

            float startY = r.y + 115;
            float rowH = 34;

            DrawScoreRow(r.x + 50, startY + rowH * 0, w - 100, "Efficiency", LastScoreReport.Efficiency);
            DrawScoreRow(r.x + 50, startY + rowH * 1, w - 100, "Creativity", LastScoreReport.Creativity);
            DrawScoreRow(r.x + 50, startY + rowH * 2, w - 100, "Resourcefulness", LastScoreReport.Resourcefulness);
            DrawScoreRow(r.x + 50, startY + rowH * 3, w - 100, "Cost Value", LastScoreReport.CostScore);
            DrawScoreRow(r.x + 50, startY + rowH * 4, w - 100, "Time Bonus", LastScoreReport.TimeScore);

            GUI.DrawTexture(new Rect(r.x + 50, startY + rowH * 5 + 6, w - 100, 2), Texture2D.whiteTexture);

            GUI.Label(new Rect(r.x + 50, startY + rowH * 5 + 14, 200, 36), "FINAL SCORE", headingStyle);
            GUI.Label(new Rect(r.x + w - 250, startY + rowH * 5 + 14, 200, 36), $"{LastScoreReport.FinalScore} / 100", scoreValueStyle);

            GUI.Label(new Rect(r.x + 30, startY + rowH * 6 + 25, w - 60, 30), $"RATING: {LastScoreReport.GradeString}", badgeStyle);
            GUI.Label(new Rect(r.x + 40, startY + rowH * 7 + 25, w - 80, 50), $"\"{LastScoreReport.EvaluationComment}\"", centeredStyle);

            if (GUI.Button(new Rect(r.x + w / 2 - 110, r.y + h - 55, 220, 42), "PLAY AGAIN", buttonStyle))
            {
                StartLevel();
            }
        }

        private void DrawScoreRow(float x, float y, float w, string label, int value)
        {
            GUI.Label(new Rect(x, y, 200, 28), label, bodyStyle);
            GUI.Label(new Rect(x + w - 100, y, 100, 28), value.ToString(), scoreValueStyle);
        }

        private void DrawInstructionsModal()
        {
            GUI.color = new Color(0, 0, 0, 0.90f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkPanelTex);
            GUI.color = Color.white;

            float w = Mathf.Min(680, Screen.width - 32);
            float h = 440;
            Rect r = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

            GUI.Box(r, GUIContent.none);
            GUI.Label(new Rect(r.x + 20, r.y + 25, w - 40, 40), "PUZZLE CRAFT", titleStyle);
            GUI.Label(new Rect(r.x + 20, r.y + 70, w - 40, 30), "Explore, Scavenge & Craft Solutions", centeredStyle);

            string text = "HOW TO PLAY:\n\n" +
                "1. OBSERVE & SEARCH: Click objects in the scene to collect them into your Inventory.\n" +
                "2. EXPERIMENT: Drag or click items into the Crafting Bench.\n" +
                "3. CRAFT: Press CRAFT to test your creation!\n" +
                "4. SCORE: Earn high scores for resourcefulness & creative solutions!\n";

            GUI.Label(new Rect(r.x + 40, r.y + 115, w - 80, 220), text, bodyStyle);

            if (GUI.Button(new Rect(r.x + w / 2 - 120, r.y + h - 65, 240, 46), "START!", buttonStyle))
            {
                StartLevel();
            }
        }

        private void DrawPauseModal()
        {
            GUI.color = new Color(0, 0, 0, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkPanelTex);
            GUI.color = Color.white;

            float w = 400;
            float h = 240;
            Rect r = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

            GUI.Box(r, GUIContent.none);
            GUI.Label(new Rect(r.x + 20, r.y + 25, w - 40, 36), "PAUSED", titleStyle);

            if (GUI.Button(new Rect(r.x + 80, r.y + 85, 240, 44), "RESUME", buttonStyle))
            {
                ResumeGame();
            }
            if (GUI.Button(new Rect(r.x + 80, r.y + 145, 240, 44), "RESTART LEVEL", buttonStyle))
            {
                StartLevel();
            }
        }
    }
}
