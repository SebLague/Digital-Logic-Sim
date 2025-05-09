using System;
using UnityEngine;

namespace DLS.ModdingAPI
{
    public class ShortcutBuilder
    {
        public readonly string modID;
        public readonly string Name;
        public KeyCode Key;
        public Func<bool> ModifierCondition;

        public ShortcutBuilder(string modID, string name)
        {
            this.modID = modID;
            Name = name;
        }

        public ShortcutBuilder SetKey(KeyCode key)
        {
            Key = key;
            return this;
        }

        public ShortcutBuilder SetModifierCondition(Func<bool> modifierCondition)
        {
            ModifierCondition = modifierCondition;
            return this;
        }
    }
}