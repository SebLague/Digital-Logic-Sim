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
			Options,
			PinRename,
			MainMenu,
			RebindKeyChip,
			Clock,
			RomEdit,
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

			switch (menuToDraw)
			{
				case MenuType.ChipSave: ChipSaveMenu.DrawMenu(); break;
				case MenuType.ChipLibrary: ChipLibraryMenu.DrawMenu(); break;
				case MenuType.ChipCustomization: ChipCustomizationMenu.DrawMenu(); break;
				case MenuType.Options: PreferencesMenu.DrawMenu(project); break;
				case MenuType.PinRename: PinEditMenu.DrawMenu(); break;
				case MenuType.RebindKeyChip: RebindKeyChipMenu.DrawMenu(); break;
				case MenuType.Clock: ClockMenu.DrawMenu(); break;
				case MenuType.RomEdit: RomEditMenu.DrawMenu(); break;
				case MenuType.UnsavedChanges: UnsavedChangesPopup.DrawMenu(); break;
				case MenuType.Search: SearchPopup.DrawMenu(); break;
				case MenuType.ChipLabelPopup: ChipLabelMenu.DrawMenu(); break;
				default:
				{
					bool showSimPausedBanner = project.simPaused;
					if (showSimPausedBanner) SimPausedUI.DrawPausedBanner();
					if (project.chipViewStack.Count > 1) ViewedChipsBar.DrawViewedChipsBanner(project, showSimPausedBanner);
					break;
				}
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

				switch (ActiveMenu)
				{
					case MenuType.ChipSave: ChipSaveMenu.OnMenuOpened(); break;
					case MenuType.ChipLibrary: ChipLibraryMenu.OnMenuOpened(); break;
					case MenuType.ChipCustomization: ChipCustomizationMenu.OnMenuOpened(); break;
					case MenuType.PinRename: PinEditMenu.OnMenuOpened(); break;
					case MenuType.Options: PreferencesMenu.OnMenuOpened(); break;
					case MenuType.MainMenu: MainMenu.OnMenuOpened(); break;
					case MenuType.RebindKeyChip: RebindKeyChipMenu.OnMenuOpened(); break;
					case MenuType.Clock: ClockMenu.OnMenuOpened(); break;
					case MenuType.RomEdit: RomEditMenu.OnMenuOpened(); break;
					case MenuType.Search: SearchPopup.OnMenuOpened(); break;
					case MenuType.ChipLabelPopup: ChipLabelMenu.OnMenuOpened(); break;
				}

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