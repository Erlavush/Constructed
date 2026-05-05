using System;
using System.Collections.Generic;
using System.Globalization;

namespace Constructed.Minecraft
{
    public sealed class BlockEntityDataBuilder
    {
        private readonly Dictionary<string, int> indicesByKey;
        private readonly List<BlockEntityDataValue> values;

        public BlockEntityDataBuilder()
        {
            indicesByKey = new Dictionary<string, int>(StringComparer.Ordinal);
            values = new List<BlockEntityDataValue>();
        }

        public void SetString(string key, string value)
        {
            BlockEntityDataValue dataValue = new BlockEntityDataValue(key, value);
            int index;
            if (indicesByKey.TryGetValue(key, out index))
            {
                values[index] = dataValue;
                return;
            }

            indicesByKey.Add(key, values.Count);
            values.Add(dataValue);
        }

        public void SetInt32(string key, int value)
        {
            SetString(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public void SetInt64(string key, long value)
        {
            SetString(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public void SetBoolean(string key, bool value)
        {
            SetString(key, value ? "true" : "false");
        }

        public BlockEntityData Build()
        {
            return new BlockEntityData(values);
        }
    }
}
