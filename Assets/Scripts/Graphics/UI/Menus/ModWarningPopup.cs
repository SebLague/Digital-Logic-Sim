using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Game;
using DLS.Mods;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace DLS.Graphics
{
    public static class ModWarningPopup
    {
        public static bool MenuShown = false;
        public static void DrawMenu()
        {
            MenuHelper.DrawBackgroundOverlay();
            Draw.ID panelID = UI.ReservePanel();
            DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

            Vector2 pos = UI.Centre + Vector2.up * (UI.HalfHeight * 0.25f);

            // Collect chip names hidden due to missing mods
            string hiddenChipNames = string.Join("\n", Project.ActiveProject.chipLibrary.allChips
                .Where(chip => chip.DependsOnModIDs != null && !chip.DependsOnModIDs.All(ModLoader.IsModLoaded))
                .Select(chip => chip.Name));
                
            // Format chip names and their dependencies
            string hiddenChipsDependencies = string.Join("\n", Project.ActiveProject.chipLibrary.allChips
                .Where(chip => chip.DependsOnModIDs != null && !chip.DependsOnModIDs.All(ModLoader.IsModLoaded))
                .Select(chip => $"{chip.Name,-30}{string.Join(", ", chip.DependsOnModIDs.Where(id => !ModLoader.IsModLoaded(id))),30}"));

            using (UI.BeginBoundsScope(true))
            {
                // Draw warning text
                UI.DrawText(
                    "Some chips contain subchips from mods that are not loaded.\nThe following chips have been disabled until their\nassociated mods have been loaded:",
                    theme.FontBold,
                    theme.FontSizeRegular,
                    pos,
                    Anchor.TextCentre,
                    Color.white
                );

                UI.DrawText(
                    $"{"Chip Name", -30}{"Mod ID", 30}",
                    theme.FontBold,
                    theme.FontSizeRegular,
                    UI.GetCurrentBoundsScope().BottomLeft + Vector2.down * 3f,
                    Anchor.TextCentreLeft,
                    Color.white
                );
                
                // Draw list of missing mod IDs
                UI.DrawText(
                    hiddenChipsDependencies,
                    theme.FontRegular,
                    theme.FontSizeRegular,
                    UI.GetCurrentBoundsScope().BottomLeft + Vector2.down * 1.5f,
                    Anchor.TextCentreLeft,
                    Color.white
                );

                bool result = UI.Button(
                    "OK",
                    theme.ButtonTheme,
                    UI.GetCurrentBoundsScope().CentreBottom  + Vector2.down * 3f,
                    size: (UI.GetCurrentBoundsScope().Width - DrawSettings.DefaultButtonSpacing * 6) * Vector2.right
                );

                MenuHelper.DrawReservedMenuPanel(panelID, UI.GetCurrentBoundsScope());

                if (result || KeyboardShortcuts.CancelShortcutTriggered)
                {
                    UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
                }
            }
        }

        public static void OnMenuOpened()
        {
            MenuShown = true;
        }
    }
}