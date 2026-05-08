using Constructed.Core;
using Constructed.Minecraft;
using System;

namespace Constructed.Create
{
    public static class WrenchableHelper
    {
        public static BlockState GetRotatedBlockState(BlockState state, Direction clickedFace)
        {
            BlockDefinition definition = state.Definition;
            BlockState newState = state;

            // 1. Handle AXIS property (standard RotatedPillarBlock/Shaft)
            if (definition.TryGetProperty("axis", out IStateProperty axisProp) && axisProp.ValueType == typeof(Axis))
            {
                Axis currentAxis = state.Get((StateProperty<Axis>)axisProp);
                // Cycle axis based on clicked face axis
                // Create logic: axisAsFace(currentAxis).getClockWise(clickedFace.getAxis()).getAxis()
                Direction currentDir = AxisToDirection(currentAxis);
                Direction rotatedDir = currentDir.ClockWise(clickedFace.Axis());
                return state.With((StateProperty<Axis>)axisProp, rotatedDir.Axis());
            }

            // 2. Handle FACING property (standard DirectionalBlock/Motor/Crate/Funnel)
            if (definition.TryGetProperty("facing", out IStateProperty facingProp) && facingProp.ValueType == typeof(Direction))
            {
                Direction currentFacing = state.Get((StateProperty<Direction>)facingProp);
                
                // If we click on the axis of the facing, we might want to cycle other properties
                // but for now, we just rotate the facing around the clicked face axis.
                if (currentFacing.Axis() == clickedFace.Axis())
                {
                    // Special case for some blocks: cycle other properties if they exist
                    // For now, just return as is if it's the same axis (like in Create for some blocks)
                    // or implement specific overrides in the blocks themselves.
                    return state;
                }
                
                return state.With((StateProperty<Direction>)facingProp, currentFacing.ClockWise(clickedFace.Axis()));
            }

            return state;
        }

        private static Direction AxisToDirection(Axis axis)
        {
            return axis switch
            {
                Axis.X => Direction.East,
                Axis.Y => Direction.Up,
                Axis.Z => Direction.South,
                _ => Direction.Up
            };
        }
    }
}
