using System;
using Constructed.Core;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class SimulationClockTests
    {
        [Test]
        public void ClockAdvancesInWholeTicks()
        {
            SimulationClock clock = new SimulationClock();

            clock.Advance();
            clock.Advance(19);

            Assert.AreEqual(20, clock.CurrentTick);
        }

        [Test]
        public void ClockRejectsInvalidTicks()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationClock(-1));

            SimulationClock clock = new SimulationClock();
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Reset(-1));
        }
    }
}
