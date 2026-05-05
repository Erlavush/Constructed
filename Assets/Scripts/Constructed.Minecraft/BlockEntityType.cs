using System;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public delegate BlockEntity BlockEntityFactory(
        BlockEntityType type,
        BlockWorld world,
        BlockPos position,
        BlockState state);

    public sealed class BlockEntityType
    {
        private readonly BlockEntityFactory factory;

        public BlockEntityType(ResourceLocation id, BlockEntityFactory factory)
        {
            if (string.IsNullOrEmpty(id.Namespace) || string.IsNullOrEmpty(id.Path))
                throw new ArgumentException("Block entity type id must be initialized.", nameof(id));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            Id = id;
            this.factory = factory;
        }

        public ResourceLocation Id { get; }

        public BlockEntity Create(BlockWorld world, BlockPos position, BlockState state)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (!ReferenceEquals(state.Definition.BlockEntityType, this))
                throw new ArgumentException($"Block state {state} does not use block entity type {Id}.", nameof(state));

            BlockEntity blockEntity = factory(this, world, position, state);
            if (blockEntity == null)
                throw new InvalidOperationException($"Block entity factory for {Id} returned null.");
            if (!ReferenceEquals(blockEntity.Type, this))
                throw new InvalidOperationException($"Block entity factory for {Id} returned an entity with type {blockEntity.Type.Id}.");
            if (!ReferenceEquals(blockEntity.World, world))
                throw new InvalidOperationException($"Block entity factory for {Id} returned an entity for the wrong world.");
            if (blockEntity.Position != position)
                throw new InvalidOperationException($"Block entity factory for {Id} returned an entity for position {blockEntity.Position} instead of {position}.");
            if (!ReferenceEquals(blockEntity.State, state))
                throw new InvalidOperationException($"Block entity factory for {Id} returned an entity for the wrong block state.");

            return blockEntity;
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
