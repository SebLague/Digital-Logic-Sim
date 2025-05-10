using DLS.Game;
using Seb.Vis.UI;

namespace DLS.Graphics
{
	public static class UIDrawer
	{
		public enum MenuType
		{
			None,
			ChipSave,
			ChipLibrary,
			BottomBarMenuPopup,
			ChipCustomization,
			Preferences,
			PinRename,
			MainMenu,
			RebindKeyChip,
			RomEdit,
			PulseEdit,
			UnsavedChanges,
			Search,
			ChipLabelPopup
		}

		static MenuType activeMenuOld;

		public static MenuType ActiveMenu { get; private set; }

		public static void Draw()
		{
			NotifyIfActiveMenuChanged();

			using (UI.CreateFixedAspectUIScope(drawLetterbox: true))
			{
				if (ActiveMenu is MenuType.MainMenu)
				{
					DrawAppMenus();
				}
				else
				{
					DrawProjectMenus(Project.ActiveProject);
				}
			}

			InteractionState.MouseIsOverUI = UI.IsMouseOverUIThisFrame;
		}

		static void DrawAppMenus()
		{
			MainMenu.Draw();
		}

		static void DrawProjectMenus(Project project)
		{
			MenuType menuToDraw = ActiveMenu; // cache state in case it changes while drawing/updating the menus

			if (menuToDraw != MenuType.ChipCustomization) BottomBarUI.DrawUI(project);

			if (menuToDraw == MenuType.ChipSave) ChipSaveMenu.DrawMenu();
			else if (menuToDraw == MenuType.ChipLibrary) ChipLibraryMenu.DrawMenu();
			else if (menuToDraw == MenuType.ChipCustomization) ChipCustomizationMenu.DrawMenu();
			else if (menuToDraw == MenuType.Preferences) PreferencesMenu.DrawMenu(project);
			else if (menuToDraw == MenuType.PinRename) PinEditMenu.DrawMenu();
			else if (menuToDraw == MenuType.RebindKeyChip) RebindKeyChipMenu.DrawMenu();
			else if (menuToDraw == MenuType.RomEdit) RomEditMenu.DrawMenu();
			else if (menuToDraw == MenuType.UnsavedChanges) UnsavedChangesPopup.DrawMenu();
			else if (menuToDraw == MenuType.Search) SearchPopup.DrawMenu();
			else if (menuToDraw == MenuType.ChipLabelPopup) ChipLabelMenu.DrawMenu();
			else if (menuToDraw == MenuType.PulseEdit) PulseEditMenu.DrawMenu();
			else
			{
				bool showSimPausedBanner = project.simPaused;
				if (showSimPausedBanner) SimPausedUI.DrawPausedBanner();
				if (project.chipViewStack.Count > 1) ViewedChipsBar.DrawViewedChipsBanner(project, showSimPausedBanner);
			}

			ContextMenu.Update();
		}

		public static bool InInputBlockingMenu() => !(ActiveMenu is MenuType.None or MenuType.BottomBarMenuPopup or MenuType.ChipCustomization);

		static void NotifyIfActiveMenuChanged()
		{
			// UI Changed -- notify opened
			if (ActiveMenu != activeMenuOld)
			{
				if (activeMenuOld == MenuType.ChipCustomization) CustomizationSceneDrawer.OnCustomizationMenuClosed();

				if (ActiveMenu == MenuType.ChipSave) ChipSaveMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.ChipLibrary) ChipLibraryMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.ChipCustomization) ChipCustomizationMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.PinRename) PinEditMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.Preferences) PreferencesMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.MainMenu) MainMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.RebindKeyChip) RebindKeyChipMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.RomEdit) RomEditMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.Search) SearchPopup.OnMenuOpened();
				else if (ActiveMenu == MenuType.ChipLabelPopup) ChipLabelMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.PulseEdit) PulseEditMenu.OnMenuOpened();

				if (InInputBlockingMenu() && Project.ActiveProject != null && Project.ActiveProject.controller != null)
				{
					Project.ActiveProject.controller.CancelEverything();
				}

				activeMenuOld = ActiveMenu;
			}
		}

		public static void ToggleBottomPopupMenu()
		{
			SetActiveMenu(ActiveMenu is MenuType.None ? MenuType.BottomBarMenuPopup : MenuType.None);
		}

		public static void SetActiveMenu(MenuType type)
		{
			ActiveMenu = type;
		}

		public static void Reset()
		{
			SetActiveMenu(MenuType.None);
			activeMenuOld = MenuType.None;
			ContextMenu.Reset();
			UI.ResetAllStates();
			BottomBarUI.Reset();
			ChipSaveMenu.Reset();
			RomEditMenu.Reset();
			ChipLibraryMenu.Reset();
			SearchPopup.Reset();
		}
	}
}