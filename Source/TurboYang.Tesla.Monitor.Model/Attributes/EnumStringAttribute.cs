using System;

namespace TurboYang.Tesla.Monitor.Model.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class EnumStringAttribute : Attribute
    {
        public String[] Values { get; }

        public EnumStringAttribute(params String[] values)
        {
            Values = values;
        }
    }
}
