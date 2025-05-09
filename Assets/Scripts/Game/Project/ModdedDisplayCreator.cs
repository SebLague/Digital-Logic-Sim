using System.Collections.Generic;
using UnityEngine;
using DLS.Description;
using DLS.ModdingAPI;
using System;

namespace DLS.Game
{
    public static class ModdedDisplayCreator
    {
        public static List<DisplayDescription> ModdedDisplays = new();
        public static Dictionary<DisplayDescription, Action<Vector2, float, uint[], uint[]>> ModdedDrawFunctions = new();

        public static DisplayDescription[] RegisterDisplays(DisplayBuilder[] displays)
        {
            List<DisplayDescription> descriptions = new();
            foreach (DisplayBuilder display in displays)
            {
                descriptions.Add(RegisterDisplay(display.Position, display.Scale, display.DrawFunction));
            }
            return descriptions.ToArray();
        }

        public static DisplayDescription RegisterDisplay(Vector2 position, float scale, Action<Vector2, float, uint[], uint[]> drawFunction)
        {
            DisplayDescription displayDescription = new(-1, position, scale);
            ModdedDisplays.Add(displayDescription);
            ModdedDrawFunctions[displayDescription] = drawFunction;
            return displayDescription;
        }

        public static bool TryGetDrawFunction(DisplayDescription displayDescription, out Action<Vector2, float, uint[], uint[]> drawFunction)
        {
            return ModdedDrawFunctions.TryGetValue(displayDescription, out drawFunction);
        }
    }
}