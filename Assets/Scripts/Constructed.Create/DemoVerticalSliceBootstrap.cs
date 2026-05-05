using System;
using System.Collections.Generic;
using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public static class DemoVerticalSliceBootstrap
    {
        public const int ChunkSize = 16;
        public const int SurfaceY = 0;
        public const int MachineY = 1;
        public const int CrateY = 2;

        public static readonly BlockPos CreativeMotorPosition = new BlockPos(2, MachineY, 8);
        public static readonly BlockPos FirstShaftPosition = new BlockPos(3, MachineY, 8);
        public static readonly BlockPos SecondShaftPosition = new BlockPos(4, MachineY, 8);
        public static readonly BlockPos BeltStartPosition = new BlockPos(5, MachineY, 8);
        public static readonly BlockPos BeltEndPosition = new BlockPos(10, MachineY, 8);
        public static readonly BlockPos CreativeCratePosition = new BlockPos(5, CrateY, 8);
        public static readonly BlockPos FunnelPosition = new BlockPos(11, MachineY, 8);
        public static readonly BlockPos ItemVaultPosition = new BlockPos(12, MachineY, 8);

        public static BlockWorld CreateFlatSurfaceWorld(DemoContentCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            BlockWorld world = new BlockWorld(catalog.Air.DefaultState);
            AddFlatSurface(world, catalog);
            return world;
        }

        public static BlockWorld CreateVerticalSliceWorld(DemoContentCatalog catalog)
        {
            BlockWorld world = CreateFlatSurfaceWorld(catalog);
            AddVerticalSlicePlacements(world, catalog);
            return world;
        }

        public static IReadOnlyList<WorldBlockEntry> CreateVerticalSlicePlacements(DemoContentCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            List<WorldBlockEntry> placements = new List<WorldBlockEntry>();
            placements.Add(new WorldBlockEntry(
                CreativeMotorPosition,
                catalog.CreativeMotor.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.East)));
            placements.Add(new WorldBlockEntry(
                FirstShaftPosition,
                catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X)));
            placements.Add(new WorldBlockEntry(
                SecondShaftPosition,
                catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X)));

            for (int x = BeltStartPosition.X; x <= BeltEndPosition.X; x++)
            {
                placements.Add(new WorldBlockEntry(
                    new BlockPos(x, MachineY, BeltStartPosition.Z),
                    catalog.Belt.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.East)));
            }

            placements.Add(new WorldBlockEntry(
                CreativeCratePosition,
                catalog.CreativeCrate.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.Down)));
            placements.Add(new WorldBlockEntry(
                FunnelPosition,
                catalog.BrassFunnel.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.East)));
            placements.Add(new WorldBlockEntry(
                ItemVaultPosition,
                catalog.ItemVault.DefaultState.With(ItemVaultBlock.HorizontalAxisProperty, Axis.X)));

            return placements;
        }

        public static void AddFlatSurface(BlockWorld world, DemoContentCatalog catalog)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            for (int x = 0; x < ChunkSize; x++)
            {
                for (int z = 0; z < ChunkSize; z++)
                    world.SetBlockState(new BlockPos(x, SurfaceY, z), catalog.Surface.DefaultState);
            }
        }

        public static void AddVerticalSlicePlacements(BlockWorld world, DemoContentCatalog catalog)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            foreach (WorldBlockEntry placement in CreateVerticalSlicePlacements(catalog))
                world.SetBlockState(placement.Position, placement.State);
        }

        public static bool IsInsideDemoChunk(BlockPos position)
        {
            return position.X >= 0 &&
                position.X < ChunkSize &&
                position.Z >= 0 &&
                position.Z < ChunkSize;
        }
    }
}
