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
        private const float BeltPreviewEmitIntervalSeconds = 0.06f;
        private const float BeltPreviewPointSpacing = 0.25f;
        private const float BeltPreviewPointJitter = 0.075f;
        private const float BeltPreviewPointSize = 0.09f;
        private const float BeltPreviewPointLifetime = 0.22f;
        private static readonly Color BeltPreviewValidColor = new Color(0.3f, 0.9f, 0.5f, 1f);
        private static readonly Color BeltPreviewInvalidColor = new Color(0.9f, 0.3f, 0.5f, 1f);

        [SerializeField]
        private float reachDistance = DefaultReachDistance;

        [SerializeField]
        private DemoVerticalSlicePresenter presenter;

        private readonly ResourceLocation?[] hotbarSlots = new ResourceLocation?[HotbarSlotCount];
        private readonly Dictionary<string, Texture2D> texturesByPath = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<ResourceLocation, BuildInventoryEntry> inventoryEntriesById = new Dictionary<ResourceLocation, BuildInventoryEntry>();
        private readonly List<BuildInventoryEntry> inventoryEntries = new List<BuildInventoryEntry>();

        private Texture2D solidTexture;
        private DemoCreativeBuildController.ModelBackedIconRenderer iconRenderer;
        private bool inventoryOpen;
        private int selectedHotbarSlotIndex;
        private BlockPos? beltFirstShaftPosition;
        private ParticleSystem beltPreviewParticleSystem;
        private float nextBeltPreviewEmitTime;

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

        public Texture2D GetHotbarSlotIconTexture(int index)
        {
            ResourceLocation? slotId = GetHotbarSlotId(index);
            if (!slotId.HasValue)
                return null;

            EnsureEntriesInitialized();
            return inventoryEntriesById.TryGetValue(slotId.Value, out BuildInventoryEntry entry)
                ? LoadEntryIconTexture(entry)
                : null;
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
            ClearBeltConnectorSelection();
            DemoMinecraftFirstPersonController playerController = GetComponentInParent<DemoMinecraftFirstPersonController>();
            if (playerController != null)
                playerController.InputEnabled = true;
        }

        private void OnDestroy()
        {
            foreach (Texture2D texture in texturesByPath.Values)
                DestroyTexture(texture);
            texturesByPath.Clear();

            if (iconRenderer != null)
            {
                iconRenderer.Dispose();
                iconRenderer = null;
            }

            DestroyTexture(solidTexture);
            solidTexture = null;

            if (beltPreviewParticleSystem != null)
            {
                if (Application.isPlaying)
                    Destroy(beltPreviewParticleSystem.gameObject);
                else
                    DestroyImmediate(beltPreviewParticleSystem.gameObject);
                beltPreviewParticleSystem = null;
            }
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
        
            ResourceLocation? selectedItemId = hotbarSlots[selectedHotbarSlotIndex];
            bool hasHit = TryRaycastWorld(camera, out DemoWorldHit hit);
            bool isSneaking = IsSneakHeld();
        
            // Special handling for Belt Connector
            bool beltConnectorSelected = selectedItemId.HasValue && selectedItemId.Value == DemoContentCatalog.BeltConnectorItemId;
            if (beltConnectorSelected)
            {
                HandleBeltConnectorInteraction(hasHit, hit);
            }
            else if (beltFirstShaftPosition.HasValue)
            {
                StopBeltPreviewParticles();
            }
        
            // Left Click: Break Block (Minecraft alignment)
            if (DemoInputSystemAdapter.WasMouseButtonPressedThisFrame(DemoMouseButton.Left))
            {
                if (beltConnectorSelected)
                    return; // Belt connector handles its own left-click for cancel/selection
                if (!hasHit)
                    return;
        
                presenter.TryRemoveBlock(hit.Position);
                return;
            }
        
            // Right Click: Use Item / Place Block / Wrench (Minecraft alignment)
            if (DemoInputSystemAdapter.WasMouseButtonPressedThisFrame(DemoMouseButton.Right))
            {
                if (!hasHit)
                    return;
        
                // Wrench Handling
                if (selectedItemId.HasValue && selectedItemId.Value == DemoContentCatalog.WrenchItemId)
                {
                    WrenchItem wrench = presenter.Catalog.Wrench;
                    BlockState state = presenter.World.GetBlockState(hit.Position);
        
                    if (isSneaking)
                    {
                        // Dismantle
                        if (wrench.Dismantle(presenter.World, hit.Position, state, hit.Face))
                        {
                            presenter.Rebuild();
                            return;
                        }
                        
                        // Default dismantle if item doesn't handle it: just remove
                        presenter.TryRemoveBlock(hit.Position);
                        return;
                    }
                    else
                    {
                        // Rotate
                        wrench.Rotate(presenter.World, hit.Position, state, hit.Face);
                        presenter.Rebuild();
                        return;
                    }
                }
        
                // Item on Block interaction (Shaft/Wrench special cases for belts)
                if (selectedItemId.HasValue && HandleItemOnBlockInteraction(selectedItemId.Value, hit))
                {
                    presenter.Rebuild();
                    return;
                }
        
                // Default Placement
                if (selectedItemId.HasValue)
                {
                    Direction nearestLookingDirection = GetNearestDirection(camera.transform.forward);
                    // Place relative to the face clicked
                    presenter.TryPlaceBlock(selectedItemId.Value, hit.Position.Relative(hit.Face), nearestLookingDirection);
                }
            }
        }

        private bool HandleItemOnBlockInteraction(ResourceLocation itemId, DemoWorldHit hit)
        {
            if (presenter == null)
                return false;

            BlockState clickedState = presenter.World.GetBlockState(hit.Position);
            if (clickedState.Definition.Id != DemoContentCatalog.BeltBlockId)
                return false;

            DemoBeltPart part = clickedState.Get(DemoContentCatalog.BeltPartProperty);

            // Shaft insertion
            if (itemId == DemoContentCatalog.ShaftBlockId && part == DemoBeltPart.Middle)
            {
                Axis beltRotationAxis = DemoBeltRuntimeResolver.GetRotationAxis(clickedState, presenter.Catalog);
                // In this demo, we assume the player places the shaft with its default orientation or we align it.
                // Create requires the shaft axis to match the belt's rotation axis.
                presenter.World.SetBlockState(hit.Position, clickedState.With(DemoContentCatalog.BeltPartProperty, DemoBeltPart.Pulley));
                return true;
            }

            // Wrench extraction
            if (itemId == DemoContentCatalog.WrenchItemId && part == DemoBeltPart.Pulley)
            {
                presenter.World.SetBlockState(hit.Position, clickedState.With(DemoContentCatalog.BeltPartProperty, DemoBeltPart.Middle));
                // We should also drop a shaft, but in creative mode we just convert back.
                // However, the lifecycle should handle restoring the physical shaft if we break it.
                return true;
            }

            return false;
        }

        private void HandleBeltConnectorInteraction(bool hasHit, DemoWorldHit hit)
        {
            if (presenter == null)
                return;

            if (beltFirstShaftPosition.HasValue &&
                !DemoBeltPlacementService.ValidateShaftEndpoint(presenter.World, presenter.Catalog, beltFirstShaftPosition.Value))
            {
                ClearBeltConnectorSelection();
            }

            if (IsSneakHeld() && DemoInputSystemAdapter.WasMouseButtonPressedThisFrame(DemoMouseButton.Left))
            {
                ClearBeltConnectorSelection();
                return;
            }

            if (beltFirstShaftPosition.HasValue)
                EmitBeltConnectorPreview(hasHit, hit);

            if (!DemoInputSystemAdapter.WasMouseButtonPressedThisFrame(DemoMouseButton.Left))
                return;
            if (!hasHit)
                return;

            BlockWorld world = presenter.World;
            DemoContentCatalog catalog = presenter.Catalog;

            if (!beltFirstShaftPosition.HasValue)
            {
                if (!DemoBeltPlacementService.ValidateShaftEndpoint(world, catalog, hit.Position))
                    return;

                beltFirstShaftPosition = hit.Position;
                nextBeltPreviewEmitTime = 0f;
                EmitBeltPreviewPoint(ToCenterPosition(hit.Position), BeltPreviewValidColor);
                return;
            }

            BlockPos firstShaft = beltFirstShaftPosition.Value;
            BlockPos secondShaft = ResolveBeltConnectorSecondShaftCandidate(hit);
            if (DemoBeltPlacementService.TryCreateConnection(world, catalog, firstShaft, secondShaft, out _))
            {
                presenter.Rebuild();
                ClearBeltConnectorSelection();
            }
        }

        private BlockPos ResolveBeltConnectorSecondShaftCandidate(DemoWorldHit hit)
        {
            if (presenter == null)
                return hit.Position;

            BlockState clickedState = presenter.World.GetBlockState(hit.Position);
            if (clickedState.Definition.Id == presenter.Catalog.Shaft.Id)
                return hit.Position;

            return hit.Position.Relative(hit.Face);
        }

        private void EmitBeltConnectorPreview(bool hasHit, DemoWorldHit hit)
        {
            if (!beltFirstShaftPosition.HasValue)
                return;
            if (Time.unscaledTime < nextBeltPreviewEmitTime)
                return;

            nextBeltPreviewEmitTime = Time.unscaledTime + BeltPreviewEmitIntervalSeconds;
            BlockPos firstShaft = beltFirstShaftPosition.Value;
            if (!hasHit)
            {
                Vector3 origin = ToCenterPosition(firstShaft);
                EmitBeltPreviewPoint(origin + UnityEngine.Random.insideUnitSphere * BeltPreviewPointJitter, BeltPreviewValidColor);
                return;
            }

            BlockPos secondShaft = ResolveBeltConnectorSecondShaftCandidate(hit);
            DemoBeltConnectionEvaluation evaluation = DemoBeltPlacementService.EvaluateConnection(
                presenter.World,
                presenter.Catalog,
                firstShaft,
                secondShaft);
            Color previewColor = evaluation.CanConnect ? BeltPreviewValidColor : BeltPreviewInvalidColor;
            EmitBeltPreviewLine(firstShaft, secondShaft, previewColor);
        }

        private void EmitBeltPreviewLine(BlockPos firstShaft, BlockPos secondShaft, Color color)
        {
            Vector3 start = ToCenterPosition(firstShaft);
            Vector3 end = ToCenterPosition(secondShaft);
            float distance = Vector3.Distance(start, end);
            if (distance <= 0.001f)
            {
                EmitBeltPreviewPoint(start, color);
                return;
            }

            int pointCount = Mathf.Clamp(Mathf.CeilToInt(distance / BeltPreviewPointSpacing) + 1, 2, 96);
            for (int i = 0; i < pointCount; i++)
            {
                float t = pointCount == 1 ? 0f : i / (float)(pointCount - 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                point += UnityEngine.Random.insideUnitSphere * BeltPreviewPointJitter;
                EmitBeltPreviewPoint(point, color);
            }
        }

        private void EmitBeltPreviewPoint(Vector3 position, Color color)
        {
            ParticleSystem particleSystem = GetOrCreateBeltPreviewParticleSystem();
            if (particleSystem == null)
                return;

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = position,
                startColor = color,
                startLifetime = BeltPreviewPointLifetime,
                startSize = BeltPreviewPointSize
            };
            particleSystem.Emit(emitParams, 1);
        }

        private ParticleSystem GetOrCreateBeltPreviewParticleSystem()
        {
            if (beltPreviewParticleSystem != null)
                return beltPreviewParticleSystem;

            GameObject particleObject = new GameObject("Belt Connector Preview Particles");
            particleObject.transform.SetParent(transform, false);
            beltPreviewParticleSystem = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = beltPreviewParticleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2048;

            ParticleSystem.EmissionModule emission = beltPreviewParticleSystem.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = beltPreviewParticleSystem.shape;
            shape.enabled = false;

            ParticleSystemRenderer renderer = beltPreviewParticleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Shader shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (shader != null)
            {
                Material material = new Material(shader);
                material.name = "Demo Belt Preview Particles";
                renderer.sharedMaterial = material;
            }

            return beltPreviewParticleSystem;
        }

        private void StopBeltPreviewParticles()
        {
            if (beltPreviewParticleSystem != null)
                beltPreviewParticleSystem.Clear();
        }

        private void ClearBeltConnectorSelection()
        {
            beltFirstShaftPosition = null;
            StopBeltPreviewParticles();
        }

        private static bool IsSneakHeld()
        {
            return DemoInputSystemAdapter.IsKeyPressed("leftShiftKey") ||
                DemoInputSystemAdapter.IsKeyPressed("rightShiftKey");
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
                    entry.ItemId == DemoContentCatalog.ShaftBlockId ||
                    entry.ItemId == DemoContentCatalog.BeltConnectorItemId ||
                    entry.ItemId == WrenchItem.WrenchItemId;
                BuildInventoryEntry inventoryEntry = new BuildInventoryEntry(
                    entry.ItemId,
                    entry.Label,
                    entry.PreviewModelId,
                    entry.PreviewTextureFile,
                    placeable);
                inventoryEntries.Add(inventoryEntry);
                inventoryEntriesById[entry.ItemId] = inventoryEntry;
            }

            BuildInventoryEntry grassBlockEntry = new BuildInventoryEntry(
                DemoContentCatalog.SurfaceBlockId,
                "Grass Block",
                ResourceLocation.Parse("minecraft:item/grass_block"),
                null,
                true);
            inventoryEntries.Add(grassBlockEntry);
            inventoryEntriesById[grassBlockEntry.Id] = grassBlockEntry;
        }

        private void EnsureDefaultHotbarInitialized()
        {
            if (!hotbarSlots[0].HasValue)
                hotbarSlots[0] = DemoContentCatalog.CreativeMotorBlockId;
            if (!hotbarSlots[1].HasValue)
                hotbarSlots[1] = DemoContentCatalog.ShaftBlockId;
            if (!hotbarSlots[2].HasValue)
                hotbarSlots[2] = DemoContentCatalog.BeltConnectorItemId;
            if (!hotbarSlots[3].HasValue)
                hotbarSlots[3] = DemoContentCatalog.WrenchItemId;
            if (!hotbarSlots[4].HasValue)
                hotbarSlots[4] = DemoContentCatalog.SurfaceBlockId;
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

            if (iconRenderer == null)
                iconRenderer = new DemoCreativeBuildController.ModelBackedIconRenderer(GetProjectRoot());

            return iconRenderer.GetIconTexture(entry);
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

        private static Vector3 ToCenterPosition(BlockPos position)
        {
            return new Vector3(position.X + 0.5f, position.Y + 0.5f, position.Z + 0.5f);
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
                ResourceLocation previewModelId,
                CreatePrivateAssetFileReference fallbackIconFile,
                bool placeable)
            {
                Id = id;
                Label = label;
                PreviewModelId = previewModelId;
                FallbackIconFile = fallbackIconFile;
                Placeable = placeable;
            }

            public ResourceLocation Id { get; }

            public string Label { get; }

            public ResourceLocation PreviewModelId { get; }

            public CreatePrivateAssetFileReference FallbackIconFile { get; }

            public bool Placeable { get; }
        }

        private sealed class ModelBackedIconRenderer : IDisposable
        {
            private const int RenderResolution = 64;
            private const float IconPaddingPixels = 6f;
            private const float FrontFaceCullThreshold = 0.0001f;
            private const float DepthEqualThreshold = 0.00001f;
            private const byte AlphaClipThreshold = 8;
            private const float ItemPreviewBaseScale = 1.6f;
            private const float MinecraftLightPower = 0.6f;
            private const float MinecraftAmbientLight = 0.4f;
            private const string MinecraftAssetRootRelativePath = "References/Minecraft-1.21.1-resources";
            private const string MinecraftTextureRootRelativePath = "References/Minecraft-1.21.1-resources/assets/minecraft/textures";

            private static readonly MinecraftModelDisplayTransform DefaultItemModelDisplay =
                new MinecraftModelDisplayTransform(new Vector3(30f, 225f, 0f), Vector3.zero, new Vector3(0.8f, 0.8f, 0.8f));
            private static readonly Color32 DefaultMinecraftGrassTint = new Color32(124, 189, 107, 255);
            private static readonly ResourceLocation GrassBlockTopTextureId = ResourceLocation.Parse("minecraft:block/grass_block_top");
            private static readonly ResourceLocation GrassBlockSideOverlayTextureId = ResourceLocation.Parse("minecraft:block/grass_block_side_overlay");
            private static readonly Vector3 MinecraftGui3DLight0 = new Vector3(-0.933439195f, -0.262694716f, -0.244300157f);
            private static readonly Vector3 MinecraftGui3DLight1 = new Vector3(-0.103571370f, -0.976606786f, 0.188446417f);
            private static readonly Vector3 MinecraftGuiFlatLight0 = new Vector3(-0.222518995f, -0.171498626f, 0.959725678f);
            private static readonly Vector3 MinecraftGuiFlatLight1 = new Vector3(-0.215012133f, -0.971825242f, 0.096567802f);

            private readonly string privateCreateAssetRoot;
            private readonly string referenceCreateAssetRoot;
            private readonly string minecraftAssetRoot;
            private readonly string minecraftTextureRoot;
            private readonly Dictionary<ResourceLocation, Texture2D> iconTexturesById =
                new Dictionary<ResourceLocation, Texture2D>();
            private readonly Dictionary<string, Texture2D> sourceTexturesByKey =
                new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

            private MinecraftModelLoader privateModelLoader;
            private MinecraftModelLoader referenceModelLoader;
            private MinecraftModelLoader minecraftModelLoader;
            private Texture2D missingTexture;

            public ModelBackedIconRenderer(string projectRoot)
            {
                if (string.IsNullOrWhiteSpace(projectRoot))
                    throw new ArgumentException("Project root cannot be empty.", nameof(projectRoot));

                privateCreateAssetRoot = CreatePrivateAssetProjectPaths.GetPrivateCreateAssetRoot(projectRoot);
                referenceCreateAssetRoot = CreatePrivateAssetProjectPaths.GetReferenceRepositoryRoot(projectRoot);
                minecraftAssetRoot = Path.Combine(projectRoot, MinecraftAssetRootRelativePath.Replace('/', Path.DirectorySeparatorChar));
                minecraftTextureRoot = Path.Combine(projectRoot, MinecraftTextureRootRelativePath.Replace('/', Path.DirectorySeparatorChar));
            }

            public Texture2D GetIconTexture(BuildInventoryEntry entry)
            {
                if (entry == null)
                    return null;

                bool isWrench = entry.Id == WrenchItem.WrenchItemId;
                if (isWrench)
                {
                    return TryCreateWrenchIcon(entry);
                }

                if (iconTexturesById.TryGetValue(entry.Id, out Texture2D existingIcon))
                    return existingIcon;

                return TryCreateModelBackedIcon(entry) ?? LoadFallbackTexture(entry.FallbackIconFile);
            }

            private Texture2D TryCreateWrenchIcon(BuildInventoryEntry entry)
            {
                float rotationAngle = (float)(Time.timeAsDouble * 180.0) % 360f;
                string cacheKey = $"wrench_{Mathf.FloorToInt(rotationAngle / 10f)}";
                ResourceLocation cacheId = ResourceLocation.Parse("create:wrench_animated");

                if (iconTexturesById.TryGetValue(cacheId, out Texture2D cached) && cached.name == cacheKey)
                    return cached;

                if (!TryLoadItemModel(entry.PreviewModelId, out MinecraftResolvedModel wrenchModel) || wrenchModel == null)
                    return null;

                ResourceLocation gearId = ResourceLocation.Parse("create:item/wrench/gear");
                if (!TryLoadItemModel(gearId, out MinecraftResolvedModel gearModel) || gearModel == null)
                    return CreateRenderedItemIcon(wrenchModel);

                Texture2D icon = CreateWrenchIcon(wrenchModel, gearModel, rotationAngle);
                if (icon != null)
                {
                    if (iconTexturesById.TryGetValue(cacheId, out Texture2D oldIcon))
                        DestroyObject(oldIcon);
                    
                    icon.name = cacheKey;
                    iconTexturesById[cacheId] = icon;
                }
                return icon;
            }

            private Texture2D CreateWrenchIcon(MinecraftResolvedModel wrenchModel, MinecraftResolvedModel gearModel, float rotationAngle)
            {
                List<SoftwareIconFace> visibleFaces = new List<SoftwareIconFace>();
                List<SoftwareIconFace> allFaces = new List<SoftwareIconFace>();
                MinecraftModelDisplayTransform display = wrenchModel.HasGuiDisplay ? wrenchModel.GuiDisplay : DefaultItemModelDisplay;
                Quaternion displayRotation = CreateMinecraftDisplayRotation(display.Rotation);
                Vector3 displayScale = Vector3.Scale(Vector3.one * ItemPreviewBaseScale, display.Scale);
                Vector3 displayTranslation = display.Translation / 16f;

                AddFacesToSoftwareIcon(wrenchModel, displayRotation, displayScale, displayTranslation, visibleFaces, allFaces);

                Quaternion gearRotation = displayRotation * Quaternion.AngleAxis(rotationAngle, Vector3.up);
                AddFacesToSoftwareIcon(gearModel, gearRotation, displayScale, displayTranslation, visibleFaces, allFaces);

                Texture2D icon = RasterizeSoftwareIcon(visibleFaces.Count > 0 ? visibleFaces : allFaces);
                if (icon != null)
                    icon.name = "Wrench Icon " + rotationAngle;
                return icon;
            }

            private void AddFacesToSoftwareIcon(
                MinecraftResolvedModel model,
                Quaternion rotation,
                Vector3 scale,
                Vector3 translation,
                List<SoftwareIconFace> visibleFaces,
                List<SoftwareIconFace> allFaces)
            {
                foreach (MinecraftModelElement element in model.Elements)
                {
                    foreach (MinecraftModelFace face in element.Faces.Values)
                    {
                        Vector3[] vertices = CreateModelFaceVertices(element, face.Direction);
                        for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                            vertices[vertexIndex] = TransformItemModelVertex(vertices[vertexIndex], rotation, scale, translation);

                        Texture2D texture = LoadModelTexture(face.TextureId);
                        if (texture == null)
                            continue;

                        Vector3 normal = CalculateFaceNormal(vertices);
                        float lightMultiplier = CalculateMinecraftGuiLighting(normal, model.UsesBlockLight);
                        SoftwareIconFace iconFace = new SoftwareIconFace(vertices, CreateModelFaceUvs(face, model.TextureSize), texture, lightMultiplier);
                        allFaces.Add(iconFace);
                        if (normal.z > FrontFaceCullThreshold)
                            visibleFaces.Add(iconFace);
                    }
                }
            }

            public void Dispose()
            {
                HashSet<Texture2D> destroyedTextures = new HashSet<Texture2D>();
                foreach (Texture2D texture in iconTexturesById.Values)
                {
                    if (texture != null && destroyedTextures.Add(texture))
                        DestroyObject(texture);
                }

                iconTexturesById.Clear();

                foreach (Texture2D texture in sourceTexturesByKey.Values)
                {
                    if (texture != null && destroyedTextures.Add(texture))
                        DestroyObject(texture);
                }

                sourceTexturesByKey.Clear();

                if (missingTexture != null && destroyedTextures.Add(missingTexture))
                {
                    DestroyObject(missingTexture);
                    missingTexture = null;
                }
            }

            private Texture2D TryCreateModelBackedIcon(BuildInventoryEntry entry)
            {
                if (!TryLoadItemModel(entry.PreviewModelId, out MinecraftResolvedModel model) || model == null)
                    return null;

                if (model.UsesGeneratedItemLayers && model.GeneratedItemTextureIds.Count > 0)
                {
                    Texture2D generatedIcon = CreateGeneratedItemIcon(model);
                    if (generatedIcon != null)
                        iconTexturesById[entry.Id] = generatedIcon;
                    return generatedIcon;
                }

                if (model.Elements.Count == 0)
                    return null;

                Texture2D renderedIcon = CreateRenderedItemIcon(model);
                if (renderedIcon != null)
                    iconTexturesById[entry.Id] = renderedIcon;
                return renderedIcon;
            }

            private bool TryLoadItemModel(ResourceLocation modelId, out MinecraftResolvedModel model)
            {
                if (TryLoadItemModel(privateCreateAssetRoot, ref privateModelLoader, modelId, out model))
                    return true;

                if (TryLoadItemModel(referenceCreateAssetRoot, ref referenceModelLoader, modelId, out model))
                    return true;

                return modelId.Namespace == "minecraft" &&
                    TryLoadItemModel(minecraftAssetRoot, ref minecraftModelLoader, modelId, out model);
            }

            private static bool TryLoadItemModel(
                string assetRoot,
                ref MinecraftModelLoader loader,
                ResourceLocation modelId,
                out MinecraftResolvedModel model)
            {
                model = null;
                if (string.IsNullOrWhiteSpace(assetRoot) || !Directory.Exists(assetRoot))
                    return false;

                try
                {
                    if (loader == null)
                        loader = new MinecraftModelLoader(assetRoot);
                    model = loader.LoadModel(modelId);
                    return model != null;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            private Texture2D CreateGeneratedItemIcon(MinecraftResolvedModel model)
            {
                List<Texture2D> layers = new List<Texture2D>();
                foreach (ResourceLocation layerTextureId in model.GeneratedItemTextureIds)
                {
                    Texture2D layerTexture = LoadModelTexture(layerTextureId);
                    if (layerTexture != null)
                        layers.Add(layerTexture);
                }

                if (layers.Count == 0)
                    return null;

                int width = layers[0].width;
                int height = layers[0].height;
                Texture2D icon = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color32[] compositePixels = new Color32[width * height];
                foreach (Texture2D layer in layers)
                {
                    if (layer.width != width || layer.height != height)
                        continue;

                    Color32[] layerPixels = layer.GetPixels32();
                    for (int pixelIndex = 0; pixelIndex < compositePixels.Length; pixelIndex++)
                        compositePixels[pixelIndex] = AlphaBlend(compositePixels[pixelIndex], layerPixels[pixelIndex]);
                }

                icon.SetPixels32(compositePixels);
                ConfigureTexture(icon);
                icon.name = "Generated Icon " + model.ModelId.Path;
                icon.Apply(false, false);
                return icon;
            }

            private Texture2D CreateRenderedItemIcon(MinecraftResolvedModel model)
            {
                List<SoftwareIconFace> visibleFaces = new List<SoftwareIconFace>();
                List<SoftwareIconFace> allFaces = new List<SoftwareIconFace>();
                MinecraftModelDisplayTransform display = model.HasGuiDisplay ? model.GuiDisplay : DefaultItemModelDisplay;
                Quaternion displayRotation = CreateMinecraftDisplayRotation(display.Rotation);
                Vector3 displayScale = Vector3.Scale(Vector3.one * ItemPreviewBaseScale, display.Scale);
                Vector3 displayTranslation = display.Translation / 16f;

                AddFacesToSoftwareIcon(model, displayRotation, displayScale, displayTranslation, visibleFaces, allFaces);

                Texture2D icon = RasterizeSoftwareIcon(visibleFaces.Count > 0 ? visibleFaces : allFaces);
                if (icon != null)
                    icon.name = "Rendered Icon " + model.ModelId.Path;
                return icon;
            }

            private Texture2D LoadModelTexture(ResourceLocation textureId)
            {
                string cacheKey = textureId.ToString();
                if (sourceTexturesByKey.TryGetValue(cacheKey, out Texture2D cachedTexture))
                    return cachedTexture;

                if (TryCreateTintedMinecraftModelTexture(textureId, out Texture2D tintedTexture))
                {
                    sourceTexturesByKey[cacheKey] = tintedTexture;
                    return tintedTexture;
                }

                string texturePath = ResolveTexturePath(textureId);
                if (!string.IsNullOrEmpty(texturePath) && File.Exists(texturePath))
                {
                    Texture2D texture = LoadTextureFromPath(texturePath);
                    if (texture != null)
                    {
                        sourceTexturesByKey[cacheKey] = texture;
                        return texture;
                    }
                }

                Texture2D missing = GetMissingTexture();
                sourceTexturesByKey[cacheKey] = missing;
                return missing;
            }

            private string ResolveTexturePath(ResourceLocation textureId)
            {
                if (textureId.Namespace == "create")
                {
                    CreatePrivateAssetFileReference file =
                        new CreatePrivateAssetFileReference(
                            CreatePrivateAssetFileReference.MainResourcesPrefix + "textures/" + textureId.Path + ".png");
                    string privatePath = CreatePrivateAssetPathResolver.ResolvePrivateAssetPath(privateCreateAssetRoot, file);
                    if (File.Exists(privatePath))
                        return privatePath;

                    string referencePath = CreatePrivateAssetPathResolver.ResolveReferenceSourcePath(referenceCreateAssetRoot, file);
                    return File.Exists(referencePath) ? referencePath : null;
                }

                if (textureId.Namespace == "minecraft")
                {
                    CreatePrivateAssetFileReference file = new CreatePrivateAssetFileReference("assets/minecraft/textures/" + textureId.Path + ".png");
                    string privatePath = CreatePrivateAssetPathResolver.ResolvePrivateAssetPath(privateCreateAssetRoot, file);
                    if (File.Exists(privatePath))
                        return privatePath;

                    return Path.Combine(
                        minecraftTextureRoot,
                        textureId.Path.Replace('/', Path.DirectorySeparatorChar) + ".png");
                }

                return null;
            }

            private bool TryCreateTintedMinecraftModelTexture(ResourceLocation textureId, out Texture2D texture)
            {
                texture = null;
                if (textureId != GrassBlockTopTextureId && textureId != GrassBlockSideOverlayTextureId)
                    return false;

                string texturePath = ResolveTexturePath(textureId);
                if (string.IsNullOrEmpty(texturePath) || !File.Exists(texturePath))
                    return false;

                Texture2D sourceTexture = LoadTextureFromPath(texturePath);
                if (sourceTexture == null)
                    return false;

                texture = CreateTintedTexture(sourceTexture, DefaultMinecraftGrassTint);
                texture.name = "Tinted " + textureId.Path;
                return true;
            }

            private Texture2D LoadFallbackTexture(CreatePrivateAssetFileReference fallbackIconFile)
            {
                if (fallbackIconFile == null)
                    return GetMissingTexture();

                string privatePath = CreatePrivateAssetPathResolver.ResolvePrivateAssetPath(privateCreateAssetRoot, fallbackIconFile);
                if (File.Exists(privatePath))
                    return LoadTextureFromPath(privatePath);

                string referencePath = CreatePrivateAssetPathResolver.ResolveReferenceSourcePath(referenceCreateAssetRoot, fallbackIconFile);
                return File.Exists(referencePath) ? LoadTextureFromPath(referencePath) : GetMissingTexture();
            }

            private Texture2D LoadTextureFromPath(string fullPath)
            {
                if (string.IsNullOrWhiteSpace(fullPath))
                    return null;

                if (sourceTexturesByKey.TryGetValue(fullPath, out Texture2D existingTexture))
                    return existingTexture;

                byte[] bytes = File.ReadAllBytes(fullPath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes, false))
                {
                    DestroyObject(texture);
                    return null;
                }

                texture.name = Path.GetFileNameWithoutExtension(fullPath);
                ConfigureTexture(texture);
                sourceTexturesByKey[fullPath] = texture;
                return texture;
            }

            private static Vector3 TransformItemModelVertex(
                Vector3 vertex,
                Quaternion displayRotation,
                Vector3 displayScale,
                Vector3 displayTranslation)
            {
                return (displayRotation * Vector3.Scale(vertex, displayScale)) + displayTranslation;
            }

            private static Quaternion CreateMinecraftDisplayRotation(Vector3 rotationDegrees)
            {
                return Quaternion.AngleAxis(rotationDegrees.x, Vector3.right) *
                    Quaternion.AngleAxis(rotationDegrees.y, Vector3.up) *
                    Quaternion.AngleAxis(rotationDegrees.z, Vector3.forward);
            }

            private static Vector3 CalculateFaceNormal(Vector3[] vertices)
            {
                return Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]).normalized;
            }

            private static Texture2D RasterizeSoftwareIcon(IReadOnlyList<SoftwareIconFace> faces)
            {
                if (faces == null || faces.Count == 0)
                    return null;
                if (!TryCalculateProjectedBounds(faces, out float minX, out float maxX, out float minY, out float maxY))
                    return null;

                float width = maxX - minX;
                float height = maxY - minY;
                if (width <= 0f || height <= 0f)
                    return null;

                Color32[] pixels = new Color32[RenderResolution * RenderResolution];
                float[] depthBuffer = new float[pixels.Length];
                for (int pixelIndex = 0; pixelIndex < depthBuffer.Length; pixelIndex++)
                    depthBuffer[pixelIndex] = float.NegativeInfinity;

                float availableSize = Mathf.Max(1f, RenderResolution - (IconPaddingPixels * 2f));
                float pixelsPerUnit = availableSize / Mathf.Max(width, height);
                Vector2 projectedCenter = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

                for (int faceIndex = 0; faceIndex < faces.Count; faceIndex++)
                {
                    SoftwareIconFace face = faces[faceIndex];
                    Vector3[] projected = ProjectFaceVertices(face.Vertices, projectedCenter, pixelsPerUnit);
                    RasterizeSoftwareTriangle(projected[0], projected[1], projected[2], face.Uvs[0], face.Uvs[1], face.Uvs[2], face.Texture, face.LightMultiplier, pixels, depthBuffer);
                    RasterizeSoftwareTriangle(projected[0], projected[2], projected[3], face.Uvs[0], face.Uvs[2], face.Uvs[3], face.Texture, face.LightMultiplier, pixels, depthBuffer);
                }

                Texture2D icon = new Texture2D(RenderResolution, RenderResolution, TextureFormat.RGBA32, false);
                icon.SetPixels32(pixels);
                ConfigureTexture(icon);
                icon.Apply(false, false);
                if (HasVisiblePixels(icon))
                    return icon;

                DestroyObject(icon);
                return null;
            }

            private static bool TryCalculateProjectedBounds(
                IReadOnlyList<SoftwareIconFace> faces,
                out float minX,
                out float maxX,
                out float minY,
                out float maxY)
            {
                minX = float.PositiveInfinity;
                maxX = float.NegativeInfinity;
                minY = float.PositiveInfinity;
                maxY = float.NegativeInfinity;
                bool hasVertices = false;
                for (int faceIndex = 0; faceIndex < faces.Count; faceIndex++)
                {
                    Vector3[] vertices = faces[faceIndex].Vertices;
                    for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                    {
                        Vector3 vertex = vertices[vertexIndex];
                        minX = Mathf.Min(minX, vertex.x);
                        maxX = Mathf.Max(maxX, vertex.x);
                        minY = Mathf.Min(minY, vertex.y);
                        maxY = Mathf.Max(maxY, vertex.y);
                        hasVertices = true;
                    }
                }

                return hasVertices;
            }

            private static Vector3[] ProjectFaceVertices(Vector3[] vertices, Vector2 projectedCenter, float pixelsPerUnit)
            {
                Vector3[] projected = new Vector3[vertices.Length];
                float halfResolution = RenderResolution * 0.5f;
                for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                {
                    Vector3 vertex = vertices[vertexIndex];
                    projected[vertexIndex] = new Vector3(
                        ((vertex.x - projectedCenter.x) * pixelsPerUnit) + halfResolution,
                        ((vertex.y - projectedCenter.y) * pixelsPerUnit) + halfResolution,
                        vertex.z);
                }

                return projected;
            }

            private static void RasterizeSoftwareTriangle(
                Vector3 a,
                Vector3 b,
                Vector3 c,
                Vector2 uvA,
                Vector2 uvB,
                Vector2 uvC,
                Texture2D texture,
                float lightMultiplier,
                Color32[] pixels,
                float[] depthBuffer)
            {
                float area = Edge(a, b, new Vector2(c.x, c.y));
                if (Mathf.Abs(area) <= 0.00001f)
                    return;

                int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, RenderResolution - 1);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, RenderResolution - 1);
                int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, RenderResolution - 1);
                int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, RenderResolution - 1);

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        Vector2 samplePosition = new Vector2(x + 0.5f, y + 0.5f);
                        float w0 = Edge(b, c, samplePosition) / area;
                        float w1 = Edge(c, a, samplePosition) / area;
                        float w2 = Edge(a, b, samplePosition) / area;
                        if (w0 < -0.0001f || w1 < -0.0001f || w2 < -0.0001f)
                            continue;

                        float depth = (a.z * w0) + (b.z * w1) + (c.z * w2);
                        int pixelIndex = (y * RenderResolution) + x;
                        float currentDepth = depthBuffer[pixelIndex];
                        if (depth < currentDepth - DepthEqualThreshold)
                            continue;

                        Vector2 uv = (uvA * w0) + (uvB * w1) + (uvC * w2);
                        Color32 sample = SampleTexturePoint(texture, uv);
                        if (sample.a <= AlphaClipThreshold)
                            continue;
                        sample = ApplyMinecraftGuiLighting(sample, lightMultiplier);

                        if (depth <= currentDepth + DepthEqualThreshold && pixels[pixelIndex].a > 0)
                        {
                            pixels[pixelIndex] = AlphaBlend(pixels[pixelIndex], sample);
                            if (depth > currentDepth)
                                depthBuffer[pixelIndex] = depth;
                        }
                        else
                        {
                            pixels[pixelIndex] = sample;
                            depthBuffer[pixelIndex] = depth;
                        }
                    }
                }
            }

            private static float Edge(Vector3 a, Vector3 b, Vector2 c)
            {
                return ((c.x - a.x) * (b.y - a.y)) - ((c.y - a.y) * (b.x - a.x));
            }

            private static Color32 SampleTexturePoint(Texture2D texture, Vector2 uv)
            {
                int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * texture.width), 0, texture.width - 1);
                int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * texture.height), 0, texture.height - 1);
                return texture.GetPixel(x, y);
            }

            private static float CalculateMinecraftGuiLighting(Vector3 normal, bool usesBlockLight)
            {
                if (normal.sqrMagnitude <= 0.000001f)
                    return 1f;

                Vector3 shaderNormal = new Vector3(normal.x, -normal.y, normal.z).normalized;
                Vector3 light0 = usesBlockLight ? MinecraftGui3DLight0 : MinecraftGuiFlatLight0;
                Vector3 light1 = usesBlockLight ? MinecraftGui3DLight1 : MinecraftGuiFlatLight1;
                float light0Contribution = Mathf.Max(0f, Vector3.Dot(light0, shaderNormal));
                float light1Contribution = Mathf.Max(0f, Vector3.Dot(light1, shaderNormal));
                return Mathf.Min(1f, ((light0Contribution + light1Contribution) * MinecraftLightPower) + MinecraftAmbientLight);
            }

            private Texture2D GetMissingTexture()
            {
                if (missingTexture != null)
                    return missingTexture;

                missingTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        bool border = x == 0 || y == 0 || x == 15 || y == 15;
                        Color pixel = border
                            ? new Color(0.88f, 0.2f, 0.16f, 1f)
                            : (((x + y) & 1) == 0 ? new Color(0f, 0f, 0f, 0f) : new Color(1f, 0f, 1f, 0.92f));
                        missingTexture.SetPixel(x, y, pixel);
                    }
                }

                ConfigureTexture(missingTexture);
                missingTexture.name = "Missing Item Icon";
                missingTexture.Apply(false, false);
                return missingTexture;
            }

            private static void ConfigureTexture(Texture2D texture)
            {
                if (texture == null)
                    return;

                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.anisoLevel = 0;
                texture.mipMapBias = 0f;
            }

            private static Vector3[] CreateModelFaceVertices(MinecraftModelElement element, Direction direction)
            {
                Vector3 from = element.From;
                Vector3 to = element.To;
                Vector3[] vertices;
                switch (direction)
                {
                    case Direction.Down:
                        vertices = new[]
                        {
                            new Vector3(from.x, from.y, from.z),
                            new Vector3(to.x, from.y, from.z),
                            new Vector3(to.x, from.y, to.z),
                            new Vector3(from.x, from.y, to.z)
                        };
                        break;
                    case Direction.Up:
                        vertices = new[]
                        {
                            new Vector3(from.x, to.y, to.z),
                            new Vector3(to.x, to.y, to.z),
                            new Vector3(to.x, to.y, from.z),
                            new Vector3(from.x, to.y, from.z)
                        };
                        break;
                    case Direction.North:
                        vertices = new[]
                        {
                            new Vector3(to.x, from.y, from.z),
                            new Vector3(from.x, from.y, from.z),
                            new Vector3(from.x, to.y, from.z),
                            new Vector3(to.x, to.y, from.z)
                        };
                        break;
                    case Direction.South:
                        vertices = new[]
                        {
                            new Vector3(from.x, from.y, to.z),
                            new Vector3(to.x, from.y, to.z),
                            new Vector3(to.x, to.y, to.z),
                            new Vector3(from.x, to.y, to.z)
                        };
                        break;
                    case Direction.West:
                        vertices = new[]
                        {
                            new Vector3(from.x, from.y, from.z),
                            new Vector3(from.x, from.y, to.z),
                            new Vector3(from.x, to.y, to.z),
                            new Vector3(from.x, to.y, from.z)
                        };
                        break;
                    case Direction.East:
                        vertices = new[]
                        {
                            new Vector3(to.x, from.y, to.z),
                            new Vector3(to.x, from.y, from.z),
                            new Vector3(to.x, to.y, from.z),
                            new Vector3(to.x, to.y, to.z)
                        };
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }

                for (int i = 0; i < vertices.Length; i++)
                {
                    if (element.Rotation != null)
                        vertices[i] = ApplyElementRotation(vertices[i], element.Rotation);

                    vertices[i] = (vertices[i] / 16f) - (Vector3.one * 0.5f);
                }

                return vertices;
            }

            private static Vector3 ApplyElementRotation(Vector3 point, MinecraftModelElementRotation rotation)
            {
                Vector3 axis;
                switch (rotation.Axis)
                {
                    case Axis.X:
                        axis = Vector3.right;
                        break;
                    case Axis.Y:
                        axis = Vector3.up;
                        break;
                    case Axis.Z:
                        axis = Vector3.forward;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return rotation.Origin + (Quaternion.AngleAxis(rotation.Angle, axis) * (point - rotation.Origin));
            }

            private static Vector2[] CreateModelFaceUvs(MinecraftModelFace face, Vector2 textureSize)
            {
                Vector2[] uvs =
                {
                    new Vector2(face.Uv.x / textureSize.x, 1f - (face.Uv.w / textureSize.y)),
                    new Vector2(face.Uv.z / textureSize.x, 1f - (face.Uv.w / textureSize.y)),
                    new Vector2(face.Uv.z / textureSize.x, 1f - (face.Uv.y / textureSize.y)),
                    new Vector2(face.Uv.x / textureSize.x, 1f - (face.Uv.y / textureSize.y))
                };

                for (int step = 0; step < (face.RotationDegrees / 90); step++)
                {
                    uvs = new[]
                    {
                        uvs[3],
                        uvs[0],
                        uvs[1],
                        uvs[2]
                    };
                }

                return uvs;
            }

            private static Texture2D CreateTintedTexture(Texture2D sourceTexture, Color32 tint)
            {
                Texture2D texture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
                Color32[] sourcePixels = sourceTexture.GetPixels32();
                Color32[] tintedPixels = new Color32[sourcePixels.Length];
                for (int i = 0; i < sourcePixels.Length; i++)
                {
                    Color32 sourcePixel = sourcePixels[i];
                    tintedPixels[i] = new Color32(
                        MultiplyByte(sourcePixel.r, tint.r),
                        MultiplyByte(sourcePixel.g, tint.g),
                        MultiplyByte(sourcePixel.b, tint.b),
                        sourcePixel.a);
                }

                texture.SetPixels32(tintedPixels);
                ConfigureTexture(texture);
                texture.Apply(false, false);
                return texture;
            }

            private static byte MultiplyByte(byte first, byte second)
            {
                return (byte)((first * second) / 255);
            }

            private static Color32 ApplyMinecraftGuiLighting(Color32 sample, float lightMultiplier)
            {
                if (lightMultiplier >= 0.9999f)
                    return sample;

                return new Color32(
                    MultiplyColorByte(sample.r, lightMultiplier),
                    MultiplyColorByte(sample.g, lightMultiplier),
                    MultiplyColorByte(sample.b, lightMultiplier),
                    sample.a);
            }

            private static byte MultiplyColorByte(byte value, float multiplier)
            {
                return (byte)Mathf.Clamp(Mathf.RoundToInt(value * multiplier), 0, 255);
            }

            private static Color32 AlphaBlend(Color32 basePixel, Color32 overlayPixel)
            {
                int overlayAlpha = overlayPixel.a;
                if (overlayAlpha == 0)
                    return basePixel;

                int inverseAlpha = 255 - overlayAlpha;
                byte red = (byte)(((basePixel.r * inverseAlpha) + (overlayPixel.r * overlayAlpha)) / 255);
                byte green = (byte)(((basePixel.g * inverseAlpha) + (overlayPixel.g * overlayAlpha)) / 255);
                byte blue = (byte)(((basePixel.b * inverseAlpha) + (overlayPixel.b * overlayAlpha)) / 255);
                byte alpha = (byte)Mathf.Clamp(basePixel.a + overlayAlpha, 0, 255);
                return new Color32(red, green, blue, alpha);
            }

            private static bool HasVisiblePixels(Texture2D texture)
            {
                if (texture == null)
                    return false;

                Color32[] pixels = texture.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a > 0)
                        return true;
                }

                return false;
            }

            private static void DestroyObject(UnityEngine.Object target)
            {
                if (target == null)
                    return;

                if (Application.isPlaying)
                    Destroy(target);
                else
                    DestroyImmediate(target);
            }

            private readonly struct SoftwareIconFace
            {
                public SoftwareIconFace(Vector3[] vertices, Vector2[] uvs, Texture2D texture, float lightMultiplier)
                {
                    Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
                    Uvs = uvs ?? throw new ArgumentNullException(nameof(uvs));
                    Texture = texture;
                    LightMultiplier = lightMultiplier;
                }

                public Vector3[] Vertices { get; }

                public Vector2[] Uvs { get; }

                public Texture2D Texture { get; }

                public float LightMultiplier { get; }
            }
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
