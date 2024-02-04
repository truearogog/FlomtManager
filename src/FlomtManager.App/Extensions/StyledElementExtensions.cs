using Avalonia;
using Avalonia.Controls;
using System;

namespace FlomtManager.App.Extensions
{
    public static class StyledElementExtensions
    {
        public static Window GetWindow(this StyledElement element)
        {
            for (var parent = element; parent != null; parent = parent.Parent)
            {
                if (parent.GetType().IsAssignableTo(typeof(Window)))
                {
                    return (Window)parent;
                }
            }
            throw new Exception("No parent window found!");
        }
    }
}
