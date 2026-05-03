using System;
using System.Collections.Generic;

namespace Constructed.Minecraft
{
    public interface IStateProperty
    {
        string Name { get; }

        Type ValueType { get; }

        object DefaultValue { get; }

        IReadOnlyList<object> ValidValues { get; }

        bool IsValidValue(object value);

        string SerializeValue(object value);

        object ParseValue(string value);
    }
}
