using System;
using System.Collections.Generic;
using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public static class DemoVerticalSliceBootstrap
    {
        public const int ChunkSize = 16;
        public const int PlatformSize = 8;
        public const int SurfaceY = 0;
        public const int MachineY = 1;

        public static readonly BlockPos CreativeMotorPosition = new BlockPos(2, MachineY, 4);
        public static readonly BlockPos FirstShaftPosition = new BlockPos(3, MachineY, 4);
        public static readonly BlockPos SecondShaftPosition = new BlockPos(4, MachineY, 4);

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

            return placements;
        }

        public static void AddFlatSurface(BlockWorld world, DemoContentCatalog catalog)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            for (int x = 0; x < PlatformSize; x++)
            {
                for (int z = 0; z < PlatformSize; z++)
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
