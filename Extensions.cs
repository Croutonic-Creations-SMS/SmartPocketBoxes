using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Configuration;
using UnityEngine;

namespace SmartPocketBoxes
{
    public static class Extensions
    {
        public static bool IsDownInclusive(this KeyboardShortcut shortcut) => Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
    }
}
