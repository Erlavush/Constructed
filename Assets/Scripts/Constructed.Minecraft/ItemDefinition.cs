using System;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public class ItemDefinition
    {
        public const int DefaultMaxStackSize = 64;

        public ItemDefinition(ResourceLocation id)
            : this(id, DefaultMaxStackSize)
        {
        }

        public ItemDefinition(ResourceLocation id, int maxStackSize)
        {
            if (string.IsNullOrEmpty(id.Namespace) || string.IsNullOrEmpty(id.Path))
                throw new ArgumentException("Item id must be initialized.", nameof(id));
            if (maxStackSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxStackSize), "Max stack size must be positive.");

            Id = id;
            MaxStackSize = maxStackSize;
        }

        public ResourceLocation Id { get; }

        public int MaxStackSize { get; }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
