using System;

namespace ApplicationModel
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ODataAction : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class ODataFunction : Attribute
    {
    }

}
