using System;
using System.Collections.Generic;
using System.IO;
using Constructed.Core;
using Constructed.Create;
using Constructed.Minecraft;
using UnityEngine;

namespace Constructed.Unity
{
    [DisallowMultipleComponent]
    public sealed class DemoCreativeBuildController : MonoBehaviour
    {
        private const float DefaultReachDistance = 8f;
        private const int HotbarSlotCount = 9;
        private const string HudTextureRoot = "References/Minecraft-1.21.1-resources/assets/minecraft/textures/gui/sprites";
        private const string HudHotbarPath = "hud/hotbar.png";
        private const string HudHotbarSelectionPath = "hud/hotbar_selection.png";
        private const string HudCrosshairPath = "hud/crosshair.png";
        private const string HudSlotPath = "container/slot.png";
        private const float InventoryPanelAlpha = 0.9f;

        [SerializeField]
        private float reachDistance = DefaultReachDistance;

        [SerializeField]
        private DemoVerticalSlicePresenter presenter;

        private readonly ResourceLocation?[] hotbarSlots = new ResourceLocation?[HotbarSlotCount];
        private readonly Dictionary<string, Texture2D> texturesByPath = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<ResourceLocation, BuildInventoryEntry> inventoryEntriesById = new Dictionary<ResourceLocation, BuildInventoryEntry>();
        private readonly List<BuildInventoryEntry> inventoryEntries = new List<BuildInventoryEntry>();

        private Texture2D solidTexture;
        private bool inventoryOpen;
        private int selectedHotbarSlotIndex;

        public int SelectedHotbarSlotIndex
        {
            get { return selectedHotbarSlotIndex; }
        }

        public bool InventoryOpen
        {
            get { return inventoryOpen; }
        }

        public int InventoryEntryCount
        {
            get { return inventoryEntries.Count; }
        }

        public ResourceLocation? GetHotbarSlotId(int index)
        {
            if (index < 0 || index >= hotbarSlots.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return hotbarSlots[index];
        }

        public void SetPresenter(DemoVerticalSlicePresenter currentPresenter)
        {
            presenter = currentPresenter;
            EnsureEntriesInitialized();
            EnsureDefaultHotbarInitialized();
        }

        private void OnEnable()
        {
            EnsureEntriesInitialized();
            EnsureDefaultHotbarInitialized();
        }

        private void OnDisable()
        {
            inventoryOpen = false;
            DemoMinecraftFirstPersonController playerController = GetComponentInParent<DemoMinecraftFirstPersonController>();
            if (playerController != null)
                playerController.InputEnabled = true;
        }

        private void OnDestroy()
        {
            foreach (Texture2D texture in texturesByPath.Values)
                DestroyTexture(texture);
            texturesByPath.Clear();

            DestroyTexture(solidTexture);
            solidTexture = null;
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            EnsureEntriesInitialized();
            EnsureDefaultHotbarInitialized();
            HandleInventoryToggle();
            UpdatePlayerInputState();
            if (inventoryOpen)
                return;

            HandleHotbarSelectionInput();
            HandleWorldInteractionInput();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
                return;

            EnsureEntriesInitialized();
            EnsureDefaultHotbarInitialized();

            float scale = CalculateUiScale();
            DrawHotbar(scale);
            if (!inventoryOpen)
                DrawCrosshair(scale);
            if (inventoryOpen)
                DrawInventory(scale);
        }

        private void UpdatePlayerInputState()
        {
            DemoMinecraftFirstPersonController playerController = GetComponentInParent<DemoMinecraftFirstPersonController>();
            if (playerController != null)
                playerController.InputEnabled = !inventoryOpen;
        }

        private void HandleInventoryToggle()
        {
            if (DemoInputSystemAdapter.WasKeyPressedThisFrame("eKey"))
            {
                inventoryOpen = !inventoryOpen;
                if (!inventoryOpen)
                {
                    DemoMinecraftFirstPersonController playerController = GetComponentInParent<DemoMinecraftFirstPersonController>();
                    playerController?.CaptureCursor();
                }

                return;
            }

            if (inventoryOpen && DemoInputSystemAdapter.WasKeyPressedThisFrame("escapeKey"))
                inventoryOpen = false;
        }

        private void HandleHotbarSelectionInput()
        {
            for (int slotIndex = 0; slotIndex < HotbarSlotCount; slotIndex++)
            {
                if (WasHotbarDigitPressed(slotIndex))
                {
                    selectedHotbarSlotIndex = slotIndex;
                    return;
                }
            }

            Vector2 scrollDelta = DemoInputSystemAdapter.ReadMouseScrollDelta();
            if (scrollDelta.y > 0.01f)
                selectedHotbarSlotIndex = (selectedHotbarSlotIndex + HotbarSlotCount - 1) % HotbarSlotCount;
            else if (scrollDelta.y < -0.01f)
                selectedHotbarSlotIndex = (selectedHotbarSlotIndex + 1) % HotbarSlotCount;
        }

        private void HandleWorldInteractionInput()
        {
            if (presenter == null)
                return;

            Camera camera = GetComponent<Camera>();
            if (camera == null)
                return;

            if (!TryRaycastWorld(camera, out DemoWorldHit hit))
                return;

            ResourceLocation? selectedBlockId = hotbarSlots[selectedHotbarSlotIndex];
            if (DemoInputSystemAdapter.WasMouseButtonPressedThisFrame(DemoMouseButton.Left))
            {
                if (!selectedBlockId.HasValue)
                    return;

                Direction nearestLookingDirection = GetNearestDirection(camera.transform.forward);
                presenter.TryPlaceBlock(selectedBlockId.Value, hit.Position.Relative(hit.Face), nearestLookingDirection);
                return;
            }

            if (!DemoInputSystemAdapter.WasMouseButtonPressedThisFrame(DemoMouseButton.Right))
                return;

            presenter.TryRemoveBlock(hit.Position);
        }

        private void DrawCrosshair(float scale)
        {
            Texture2D crosshairTexture = LoadMinecraftHudTexture(HudCrosshairPath);
            if (crosshairTexture == null)
                return;

            float size = crosshairTexture.width * scale;
            Rect rect = new Rect(
                (Screen.width * 0.5f) - (size * 0.5f),
                (Screen.height * 0.5f) - (size * 0.5f),
                size,
                crosshairTexture.height * scale);
            GUI.DrawTexture(rect, crosshairTexture, ScaleMode.StretchToFill, true);
        }

        private void DrawHotbar(float scale)
        {
            Texture2D hotbarTexture = LoadMinecraftHudTexture(HudHotbarPath);
            Texture2D selectionTexture = LoadMinecraftHudTexture(HudHotbarSelectionPath);
            if (hotbarTexture == null || selectionTexture == null)
                return;

            float hotbarWidth = hotbarTexture.width * scale;
            float hotbarHeight = hotbarTexture.height * scale;
            Rect hotbarRect = new Rect(
                (Screen.width * 0.5f) - (hotbarWidth * 0.5f),
                Screen.height - hotbarHeight - (8f * scale),
                hotbarWidth,
                hotbarHeight);

            GUI.DrawTexture(hotbarRect, hotbarTexture, ScaleMode.StretchToFill, true);

            Rect selectionRect = new Rect(
                hotbarRect.x - scale + (selectedHotbarSlotIndex * 20f * scale),
                hotbarRect.y - scale,
                selectionTexture.width * scale,
                selectionTexture.height * scale);
            GUI.DrawTexture(selectionRect, selectionTexture, ScaleMode.StretchToFill, true);

            for (int slotIndex = 0; slotIndex < HotbarSlotCount; slotIndex++)
            {
                ResourceLocation? hotbarId = hotbarSlots[slotIndex];
                if (!hotbarId.HasValue || !inventoryEntriesById.TryGetValue(hotbarId.Value, out BuildInventoryEntry entry))
                    continue;

                Texture2D iconTexture = LoadEntryIconTexture(entry);
                if (iconTexture == null)
                    continue;

                Rect iconRect = new Rect(
                    hotbarRect.x + (3f * scale) + (slotIndex * 20f * scale),
                    hotbarRect.y + (3f * scale),
                    16f * scale,
                    16f * scale);
                GUI.DrawTexture(iconRect, iconTexture, ScaleMode.StretchToFill, true);
            }

            BuildInventoryEntry selectedEntry = GetSelectedHotbarEntry();
            if (selectedEntry == null)
                return;

            GUIStyle labelStyle = GetCenteredLabelStyle((int)(12f * scale), Color.white);
            Rect labelRect = new Rect(0f, hotbarRect.y - (28f * scale), Screen.width, 20f * scale);
            string suffix = selectedEntry.Placeable ? string.Empty : " (Display Only)";
            GUI.Label(labelRect, selectedEntry.Label + suffix, labelStyle);
        }

        private void DrawInventory(float scale)
        {
            Texture2D slotTexture = LoadMinecraftHudTexture(HudSlotPath);
            if (slotTexture == null)
                return;

            float slotSize = slotTexture.width * scale;
            float gridWidth = slotSize * 9f;
            float gridHeight = slotSize * 5f;
            float panelPadding = 10f * scale;
            float panelWidth = gridWidth + (panelPadding * 2f);
            float panelHeight = gridHeight + (panelPadding * 2f) + (54f * scale);
            Rect panelRect = new Rect(
                (Screen.width * 0.5f) - (panelWidth * 0.5f),
                (Screen.height * 0.5f) - (panelHeight * 0.5f),
                panelWidth,
                panelHeight);

            DrawSolidRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.45f));
            DrawSolidRect(panelRect, new Color(0.09f, 0.09f, 0.09f, InventoryPanelAlpha));

            GUIStyle titleStyle = GetCenteredLabelStyle((int)(14f * scale), Color.white);
            GUIStyle subtitleStyle = GetCenteredLabelStyle((int)(10f * scale), new Color(0.82f, 0.82f, 0.82f, 1f));
            GUI.Label(new Rect(panelRect.x, panelRect.y + (6f * scale), panelRect.width, 20f * scale), "Creative Inventory", titleStyle);
            GUI.Label(
                new Rect(panelRect.x, panelRect.y + (22f * scale), panelRect.width, 16f * scale),
                "Click a placeable entry to assign it to the selected hotbar slot.",
                subtitleStyle);

            float gridStartX = panelRect.x + panelPadding;
            float gridStartY = panelRect.y + (42f * scale);
            Vector2 mousePosition = Event.current.mousePosition;
            BuildInventoryEntry hoveredEntry = null;

            for (int row = 0; row < 5; row++)
            {
                for (int column = 0; column < 9; column++)
                {
                    int entryIndex = (row * 9) + column;
                    Rect slotRect = new Rect(
                        gridStartX + (column * slotSize),
                        gridStartY + (row * slotSize),
                        slotSize,
                        slotSize);

                    GUI.DrawTexture(slotRect, slotTexture, ScaleMode.StretchToFill, true);

                    if (entryIndex >= inventoryEntries.Count)
                        continue;

                    BuildInventoryEntry entry = inventoryEntries[entryIndex];
                    Texture2D iconTexture = LoadEntryIconTexture(entry);
                    if (iconTexture != null)
                    {
                        Color previousColor = GUI.color;
                        if (!entry.Placeable)
                            GUI.color = new Color(1f, 1f, 1f, 0.45f);
                        GUI.DrawTexture(new Rect(slotRect.x + scale, slotRect.y + scale, 16f * scale, 16f * scale), iconTexture, ScaleMode.StretchToFill, true);
                        GUI.color = previousColor;
                    }

                    if (!slotRect.Contains(mousePosition))
                        continue;

                    hoveredEntry = entry;
                    DrawSolidRect(slotRect, new Color(1f, 1f, 1f, 0.08f));

                    if (entry.Placeable &&
                        Event.current.type == EventType.MouseDown &&
                        Event.current.button == 0)
                    {
                        hotbarSlots[selectedHotbarSlotIndex] = entry.Id;
                        Event.current.Use();
                    }
                }
            }

            string footerText = hoveredEntry != null
                ? hoveredEntry.Placeable
                    ? hoveredEntry.Label
                    : hoveredEntry.Label + " is display-only in the current build slice."
                : "Selected slot: " + (selectedHotbarSlotIndex + 1);
            GUI.Label(
                new Rect(panelRect.x, panelRect.yMax - (20f * scale), panelRect.width, 16f * scale),
                footerText,
                subtitleStyle);
        }

        private void EnsureEntriesInitialized()
        {
            if (inventoryEntries.Count > 0)
                return;

            foreach (CreateItemVisualCatalogEntry entry in CreateFirstSliceItemVisualCatalog.Entries)
            {
                bool placeable = entry.ItemId == DemoContentCatalog.CreativeMotorBlockId ||
                    entry.ItemId == DemoContentCatalog.ShaftBlockId;
                BuildInventoryEntry inventoryEntry = new BuildInventoryEntry(entry.ItemId, entry.Label, entry.PreviewTextureFile, placeable);
                inventoryEntries.Add(inventoryEntry);
                inventoryEntriesById[entry.ItemId] = inventoryEntry;
            }
        }

        private void EnsureDefaultHotbarInitialized()
        {
            if (hotbarSlots[0].HasValue || hotbarSlots[1].HasValue)
                return;

            hotbarSlots[0] = DemoContentCatalog.CreativeMotorBlockId;
            hotbarSlots[1] = DemoContentCatalog.ShaftBlockId;
        }

        private bool TryRaycastWorld(Camera camera, out DemoWorldHit hit)
        {
            hit = default;
            if (presenter == null || camera == null)
                return false;

            BlockWorld world = presenter.World;
            if (world == null)
                return false;

            Vector3 origin = camera.transform.position;
            Vector3 direction = camera.transform.forward;
            if (direction.sqrMagnitude <= 0f)
                return false;

            direction.Normalize();
            BlockPos current = ToBlockPos(origin);
            Direction enteredFace = Direction.Up;

            int stepX = direction.x > 0f ? 1 : direction.x < 0f ? -1 : 0;
            int stepY = direction.y > 0f ? 1 : direction.y < 0f ? -1 : 0;
            int stepZ = direction.z > 0f ? 1 : direction.z < 0f ? -1 : 0;

            float nextX = stepX > 0 ? current.X + 1 : current.X;
            float nextY = stepY > 0 ? current.Y + 1 : current.Y;
            float nextZ = stepZ > 0 ? current.Z + 1 : current.Z;

            float tMaxX = stepX == 0 ? float.PositiveInfinity : (nextX - origin.x) / direction.x;
            float tMaxY = stepY == 0 ? float.PositiveInfinity : (nextY - origin.y) / direction.y;
            float tMaxZ = stepZ == 0 ? float.PositiveInfinity : (nextZ - origin.z) / direction.z;

            float tDeltaX = stepX == 0 ? float.PositiveInfinity : Mathf.Abs(1f / direction.x);
            float tDeltaY = stepY == 0 ? float.PositiveInfinity : Mathf.Abs(1f / direction.y);
            float tDeltaZ = stepZ == 0 ? float.PositiveInfinity : Mathf.Abs(1f / direction.z);

            float traveled = 0f;
            while (traveled <= reachDistance)
            {
                BlockState state = world.GetBlockState(current);
                if (!world.IsAir(state))
                {
                    hit = new DemoWorldHit(current, enteredFace, traveled);
                    return true;
                }

                if (tMaxX < tMaxY)
                {
                    if (tMaxX < tMaxZ)
                    {
                        current = current.Offset(stepX, 0, 0);
                        traveled = tMaxX;
                        tMaxX += tDeltaX;
                        enteredFace = stepX > 0 ? Direction.West : Direction.East;
                    }
                    else
                    {
                        current = current.Offset(0, 0, stepZ);
                        traveled = tMaxZ;
                        tMaxZ += tDeltaZ;
                        enteredFace = stepZ > 0 ? Direction.North : Direction.South;
                    }
                }
                else
                {
                    if (tMaxY < tMaxZ)
                    {
                        current = current.Offset(0, stepY, 0);
                        traveled = tMaxY;
                        tMaxY += tDeltaY;
                        enteredFace = stepY > 0 ? Direction.Down : Direction.Up;
                    }
                    else
                    {
                        current = current.Offset(0, 0, stepZ);
                        traveled = tMaxZ;
                        tMaxZ += tDeltaZ;
                        enteredFace = stepZ > 0 ? Direction.North : Direction.South;
                    }
                }
            }

            return false;
        }

        private BuildInventoryEntry GetSelectedHotbarEntry()
        {
            ResourceLocation? hotbarId = hotbarSlots[selectedHotbarSlotIndex];
            if (!hotbarId.HasValue)
                return null;

            inventoryEntriesById.TryGetValue(hotbarId.Value, out BuildInventoryEntry entry);
            return entry;
        }

        private Texture2D LoadEntryIconTexture(BuildInventoryEntry entry)
        {
            if (entry == null)
                return null;

            string projectRoot = GetProjectRoot();
            string privateAssetRoot = CreatePrivateAssetProjectPaths.GetPrivateCreateAssetRoot(projectRoot);
            string referenceRepositoryRoot = CreatePrivateAssetProjectPaths.GetReferenceRepositoryRoot(projectRoot);
            string privatePath = CreatePrivateAssetPathResolver.ResolvePrivateAssetPath(privateAssetRoot, entry.IconFile);
            if (File.Exists(privatePath))
                return LoadTexture(privatePath);

            string referencePath = CreatePrivateAssetPathResolver.ResolveReferenceSourcePath(referenceRepositoryRoot, entry.IconFile);
            return File.Exists(referencePath) ? LoadTexture(referencePath) : null;
        }

        private Texture2D LoadMinecraftHudTexture(string relativePath)
        {
            string projectRoot = GetProjectRoot();
            string fullPath = Path.Combine(projectRoot, HudTextureRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(fullPath) ? LoadTexture(fullPath) : null;
        }

        private Texture2D LoadTexture(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return null;
            if (texturesByPath.TryGetValue(fullPath, out Texture2D existing))
                return existing;

            byte[] bytes = File.ReadAllBytes(fullPath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes, false))
            {
                DestroyTexture(texture);
                return null;
            }

            texture.name = Path.GetFileNameWithoutExtension(fullPath);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.anisoLevel = 0;
            texturesByPath.Add(fullPath, texture);
            return texture;
        }

        private void DrawSolidRect(Rect rect, Color color)
        {
            if (solidTexture == null)
            {
                solidTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                solidTexture.name = "Demo Creative UI Solid";
                solidTexture.SetPixel(0, 0, Color.white);
                solidTexture.Apply(false, true);
            }

            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, solidTexture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private static GUIStyle GetCenteredLabelStyle(int fontSize, Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = fontSize;
            style.normal.textColor = color;
            return style;
        }

        private static float CalculateUiScale()
        {
            return Mathf.Clamp(Mathf.Round(Screen.height / 360f), 2f, 4f);
        }

        private static bool WasHotbarDigitPressed(int slotIndex)
        {
            string digitKey = slotIndex switch
            {
                0 => "digit1Key",
                1 => "digit2Key",
                2 => "digit3Key",
                3 => "digit4Key",
                4 => "digit5Key",
                5 => "digit6Key",
                6 => "digit7Key",
                7 => "digit8Key",
                8 => "digit9Key",
                _ => throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, null)
            };

            string numpadKey = slotIndex switch
            {
                0 => "numpad1Key",
                1 => "numpad2Key",
                2 => "numpad3Key",
                3 => "numpad4Key",
                4 => "numpad5Key",
                5 => "numpad6Key",
                6 => "numpad7Key",
                7 => "numpad8Key",
                8 => "numpad9Key",
                _ => throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, null)
            };

            return DemoInputSystemAdapter.WasKeyPressedThisFrame(digitKey) ||
                DemoInputSystemAdapter.WasKeyPressedThisFrame(numpadKey);
        }

        private static Direction GetNearestDirection(Vector3 vector)
        {
            Vector3 normalized = vector.normalized;
            Direction bestDirection = Direction.South;
            float bestDot = float.NegativeInfinity;

            Evaluate(Direction.Up, Vector3.up);
            Evaluate(Direction.Down, Vector3.down);
            Evaluate(Direction.North, Vector3.back);
            Evaluate(Direction.South, Vector3.forward);
            Evaluate(Direction.West, Vector3.left);
            Evaluate(Direction.East, Vector3.right);

            return bestDirection;

            void Evaluate(Direction direction, Vector3 directionVector)
            {
                float dot = Vector3.Dot(normalized, directionVector);
                if (dot <= bestDot)
                    return;

                bestDot = dot;
                bestDirection = direction;
            }
        }

        private static BlockPos ToBlockPos(Vector3 worldPosition)
        {
            return new BlockPos(
                Mathf.FloorToInt(worldPosition.x),
                Mathf.FloorToInt(worldPosition.y),
                Mathf.FloorToInt(worldPosition.z));
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture == null)
                return;

            if (Application.isPlaying)
                Destroy(texture);
            else
                DestroyImmediate(texture);
        }

        private sealed class BuildInventoryEntry
        {
            public BuildInventoryEntry(
                ResourceLocation id,
                string label,
                CreatePrivateAssetFileReference iconFile,
                bool placeable)
            {
                Id = id;
                Label = label;
                IconFile = iconFile ?? throw new ArgumentNullException(nameof(iconFile));
                Placeable = placeable;
            }

            public ResourceLocation Id { get; }

            public string Label { get; }

            public CreatePrivateAssetFileReference IconFile { get; }

            public bool Placeable { get; }
        }

        private readonly struct DemoWorldHit
        {
            public DemoWorldHit(BlockPos position, Direction face, float distance)
            {
                Position = position;
                Face = face;
                Distance = distance;
            }

            public BlockPos Position { get; }

            public Direction Face { get; }

            public float Distance { get; }
        }
    }
}
