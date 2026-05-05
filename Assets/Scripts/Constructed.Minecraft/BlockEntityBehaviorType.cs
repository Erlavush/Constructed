using System;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public abstract class BlockEntityBehaviorType : IEquatable<BlockEntityBehaviorType>
    {
        protected BlockEntityBehaviorType(ResourceLocation id, Type behaviorClrType)
        {
            if (string.IsNullOrEmpty(id.Namespace) || string.IsNullOrEmpty(id.Path))
                throw new ArgumentException("Block entity behavior type id must be initialized.", nameof(id));
            if (behaviorClrType == null)
                throw new ArgumentNullException(nameof(behaviorClrType));
            if (!typeof(BlockEntityBehavior).IsAssignableFrom(behaviorClrType))
                throw new ArgumentException($"Behavior CLR type {behaviorClrType} must derive from {typeof(BlockEntityBehavior)}.", nameof(behaviorClrType));

            Id = id;
            BehaviorClrType = behaviorClrType;
        }

        public ResourceLocation Id { get; }

        internal Type BehaviorClrType { get; }

        public bool Equals(BlockEntityBehaviorType other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return Id == other.Id && BehaviorClrType == other.BehaviorClrType;
        }

        public override bool Equals(object obj)
        {
            return obj is BlockEntityBehaviorType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, BehaviorClrType);
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }

    public sealed class BlockEntityBehaviorType<TBehavior> : BlockEntityBehaviorType
        where TBehavior : BlockEntityBehavior
    {
        public BlockEntityBehaviorType(ResourceLocation id)
            : base(id, typeof(TBehavior))
        {
        }
    }
}
