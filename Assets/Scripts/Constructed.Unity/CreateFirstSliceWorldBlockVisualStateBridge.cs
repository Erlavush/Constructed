using System;
using Constructed.Core;
using Constructed.Create;
using Constructed.Minecraft;

namespace Constructed.Unity
{
    public static class CreateFirstSliceWorldBlockVisualStateBridge
    {
        private static readonly ResourceLocation ShaftId = ResourceLocation.Parse("create:shaft");
        private static readonly ResourceLocation CreativeMotorId = ResourceLocation.Parse("create:creative_motor");
        private static readonly ResourceLocation BeltId = ResourceLocation.Parse("create:belt");
        private static readonly ResourceLocation CreativeCrateId = ResourceLocation.Parse("create:creative_crate");
        private static readonly ResourceLocation BrassFunnelId = ResourceLocation.Parse("create:brass_funnel");
        private static readonly ResourceLocation ItemVaultId = ResourceLocation.Parse("create:item_vault");

        public static bool TryResolve(BlockState state, out BlockStatePropertyValue[] visualProperties)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            ResourceLocation blockId = state.Definition.Id;
            if (blockId == ShaftId)
            {
                visualProperties = new[]
                {
                    SerializeRequiredProperty(state, DemoContentCatalog.AxisProperty, DemoContentCatalog.AxisProperty.Name)
                };
                return true;
            }

            if (blockId == CreativeMotorId || blockId == CreativeCrateId)
            {
                visualProperties = new[]
                {
                    SerializeRequiredProperty(state, DemoContentCatalog.FacingProperty, DemoContentCatalog.FacingProperty.Name)
                };
                return true;
            }

            if (blockId == BeltId)
            {
                visualProperties = new[]
                {
                    SerializeRequiredProperty(state, DemoContentCatalog.BeltCasingProperty, DemoContentCatalog.BeltCasingProperty.Name),
                    SerializeRequiredProperty(state, DemoContentCatalog.BeltFacingProperty, DemoContentCatalog.BeltFacingProperty.Name),
                    SerializeRequiredProperty(state, DemoContentCatalog.BeltPartProperty, DemoContentCatalog.BeltPartProperty.Name),
                    SerializeRequiredProperty(state, DemoContentCatalog.BeltSlopeProperty, DemoContentCatalog.BeltSlopeProperty.Name),
                    SerializeRequiredProperty(state, DemoContentCatalog.BeltWaterloggedProperty, DemoContentCatalog.BeltWaterloggedProperty.Name)
                };
                return true;
            }

            if (blockId == BrassFunnelId)
            {
                visualProperties = new[]
                {
                    SerializeOptionalProperty(state, "extracting", "extracting", "false"),
                    SerializeRequiredProperty(state, DemoContentCatalog.FacingProperty, DemoContentCatalog.FacingProperty.Name),
                    SerializeOptionalProperty(state, "powered", "powered", "false"),
                    SerializeOptionalProperty(state, "waterlogged", "waterlogged", "false")
                };
                return true;
            }

            if (blockId == ItemVaultId)
            {
                visualProperties = new[]
                {
                    SerializeAliasedRequiredProperty(state, "axis", ItemVaultBlock.HorizontalAxisProperty.Name, "axis"),
                    SerializeRequiredProperty(state, ItemVaultBlock.LargeProperty, ItemVaultBlock.LargeProperty.Name)
                };
                return true;
            }

            visualProperties = null;
            return false;
        }

        private static BlockStatePropertyValue SerializeRequiredProperty<T>(
            BlockState state,
            StateProperty<T> property,
            string visualPropertyName)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (property == null)
                throw new ArgumentNullException(nameof(property));
            if (string.IsNullOrWhiteSpace(visualPropertyName))
                throw new ArgumentException("Visual property name cannot be empty.", nameof(visualPropertyName));
            if (!state.Definition.HasProperty(property))
            {
                throw new InvalidOperationException(
                    "Block state " + state.Definition.Id + " is missing required property " + property.Name + " for world visual selection.");
            }

            return new BlockStatePropertyValue(visualPropertyName, property.Serialize(state.Get(property)));
        }

        private static BlockStatePropertyValue SerializeAliasedRequiredProperty(
            BlockState state,
            string visualPropertyName,
            string runtimePropertyName,
            string aliasRuntimePropertyName)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (string.IsNullOrWhiteSpace(visualPropertyName))
                throw new ArgumentException("Visual property name cannot be empty.", nameof(visualPropertyName));
            if (TrySerializeNamedProperty(state, visualPropertyName, runtimePropertyName, out BlockStatePropertyValue value))
                return value;
            if (TrySerializeNamedProperty(state, visualPropertyName, aliasRuntimePropertyName, out value))
                return value;

            throw new InvalidOperationException(
                "Block state " + state.Definition.Id + " is missing required runtime property " + runtimePropertyName +
                " (alias " + aliasRuntimePropertyName + ") for world visual selection.");
        }

        private static BlockStatePropertyValue SerializeOptionalProperty(
            BlockState state,
            string visualPropertyName,
            string runtimePropertyName,
            string defaultSerializedValue)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (string.IsNullOrWhiteSpace(visualPropertyName))
                throw new ArgumentException("Visual property name cannot be empty.", nameof(visualPropertyName));
            if (string.IsNullOrWhiteSpace(runtimePropertyName))
                throw new ArgumentException("Runtime property name cannot be empty.", nameof(runtimePropertyName));
            if (string.IsNullOrWhiteSpace(defaultSerializedValue))
                throw new ArgumentException("Default serialized value cannot be empty.", nameof(defaultSerializedValue));
            if (TrySerializeNamedProperty(state, visualPropertyName, runtimePropertyName, out BlockStatePropertyValue value))
                return value;

            return new BlockStatePropertyValue(visualPropertyName, defaultSerializedValue);
        }

        private static bool TrySerializeNamedProperty(
            BlockState state,
            string visualPropertyName,
            string runtimePropertyName,
            out BlockStatePropertyValue value)
        {
            if (state.Definition.TryGetProperty(runtimePropertyName, out IStateProperty property))
            {
                value = new BlockStatePropertyValue(visualPropertyName, property.SerializeValue(state.GetValue(property)));
                return true;
            }

            value = default;
            return false;
        }
    }
}
