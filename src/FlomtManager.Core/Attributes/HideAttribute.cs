using FlomtManager.Core.Enums;

namespace FlomtManager.Core.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    public class HideAttribute(HideTargets hideTargets = HideTargets.All) : Attribute
    {
        public HideTargets HideTargets { get; } = hideTargets;

        public bool Hide(HideTargets hideTargets)
        {
            return (HideTargets & hideTargets) > 0;
        }
    }
}
