using System;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public readonly struct ScheduledBlockTick
    {
        public ScheduledBlockTick(
            BlockWorld world,
            BlockPos position,
            BlockDefinition scheduledBlock,
            BlockState state,
            long scheduledAtTick,
            long triggerTick,
            ScheduledTickPriority priority,
            long order)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (scheduledBlock == null)
                throw new ArgumentNullException(nameof(scheduledBlock));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (scheduledAtTick < 0)
                throw new ArgumentOutOfRangeException(nameof(scheduledAtTick), "Scheduled tick cannot be negative.");
            if (triggerTick < 0)
                throw new ArgumentOutOfRangeException(nameof(triggerTick), "Trigger tick cannot be negative.");
            if (order < 0)
                throw new ArgumentOutOfRangeException(nameof(order), "Scheduled tick order cannot be negative.");

            World = world;
            Position = position;
            ScheduledBlock = scheduledBlock;
            State = state;
            ScheduledAtTick = scheduledAtTick;
            TriggerTick = triggerTick;
            Priority = priority;
            Order = order;
        }

        public BlockWorld World { get; }

        public BlockPos Position { get; }

        public BlockDefinition ScheduledBlock { get; }

        public BlockState State { get; }

        public long ScheduledAtTick { get; }

        public long TriggerTick { get; }

        public ScheduledTickPriority Priority { get; }

        public long Order { get; }
    }
}
