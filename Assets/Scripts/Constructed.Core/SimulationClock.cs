using System;

namespace Constructed.Core
{
    public sealed class SimulationClock
    {
        public SimulationClock(long initialTick = 0)
        {
            if (initialTick < 0)
                throw new ArgumentOutOfRangeException(nameof(initialTick), "Initial tick cannot be negative.");

            CurrentTick = initialTick;
        }

        public long CurrentTick { get; private set; }

        public void Advance(long ticks = 1)
        {
            if (ticks <= 0)
                throw new ArgumentOutOfRangeException(nameof(ticks), "Advance amount must be positive.");

            checked
            {
                CurrentTick += ticks;
            }
        }

        public void Reset(long tick = 0)
        {
            if (tick < 0)
                throw new ArgumentOutOfRangeException(nameof(tick), "Reset tick cannot be negative.");

            CurrentTick = tick;
        }
    }
}
