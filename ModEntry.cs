using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI;

using StardewValley;
using StardewValley.Menus;

using Leclair.Stardew.ThemeManager;
using GenericModConfigMenu;

namespace ThemeManager
{
	public class ModEntry : Mod
	{
		// Configuration
		ModConfig Config;

		// Themes
		ThemeManager<ThemeData> ThemeManager;
		ThemeData Theme => ThemeManager.Theme;

		private Texture2D Background;

		#region Entry

		public override void Entry(IModHelper helper)
		{
			// Load Config
			Config = Helper.ReadConfig<ModConfig>();

			// Events
			Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;

			Helper.Events.Display.RenderedHud += Display_RenderedHud;

			// Set up Theme Manager
			ThemeManager = new ThemeManager<ThemeData>(this, Config.Theme);
			ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
			ThemeManager.Discover();
		}

		#endregion

		#region Events

		private void Display_RenderedHud(object sender, StardewModdingAPI.Events.RenderedHudEventArgs e)
		{
			// Read values from our theme!
			float scale = Theme.TextScale;
			Color color = Theme.TextColor ?? Game1.textColor;

			// Set up the text!
			string text = $"Hello!\n\nSelected Theme: {ThemeManager.SelectedThemeId}\nActive Theme: {ThemeManager.ActiveThemeId}";
			var size = Game1.smallFont.MeasureString(text) * scale;

			// Draw a box! Not just any box, but a box using our
			// Background texture.
			IClickableMenu.drawTextureBox(
				e.SpriteBatch,
				texture: Background,
				sourceRect: new Rectangle(0, 0, 15, 15),
				x: 16, y: 16,
				width: 48 + (int) size.X,
				height: 48 + (int) size.Y,
				color: Color.White,
				scale: 4f
			);

			// Now draw our text in the box, using the color
			// and scale from our theme.
			e.SpriteBatch.DrawString(
				Game1.smallFont,
				text,
				new Vector2(40, 40),
				color,
				0f,
				Vector2.Zero,
				scale,
				SpriteEffects.None,
				1f
			);
		}

		private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
		{
			// Load our texture and save it, since we use it a lot.
			Background = ThemeManager.Load<Texture2D>("Background.png");

			// Register our config with GMCM.
			RegisterConfig();

			// Commands
			Helper.ConsoleCommands.Add("tm_theme", "View available themes, reload them, or change theme.", (_, args) =>
			{
				// Try running the theme command.
				if (ThemeManager.PerformThemeCommand(args))
				{
					// If it returns true, that means a new theme was selected
					// so we should update our config.
					Config.Theme = ThemeManager.SelectedThemeId;
					SaveConfig();
				}
			});

			Helper.ConsoleCommands.Add("tm_retheme", "Reload themes.", ThemeManager.PerformReloadCommand);
		}

		private void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs<ThemeData> e)
		{
			// Oh no, the theme changed! Reload our texture so it's up to date.
			Background = ThemeManager.Load<Texture2D>("Background.png");
		}

		#endregion

		#region Configuration

		public void SaveConfig()
		{
			Helper.WriteConfig(Config);
		}

		public void ResetConfig()
		{
			Config = new ModConfig();
		}

		public void RegisterConfig()
		{
			if (!Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu"))
				return;

			var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
			if (api is null)
				return;

			api.Register(ModManifest, ResetConfig, SaveConfig);

			// To add a theme picker, first we want to get the dictionary of
			// valid choices from the theme manager.
			Dictionary<string, string> choices = ThemeManager.GetThemeChoices();

			// Now, we add a text option with allowedValues so that GMCM shows
			// the user a nice convenient text box.
			api.AddTextOption(
				ModManifest,
				getValue: () => Config.Theme,
				setValue: val =>
				{
					// Here, we both want to set the value to our config object
					// but also select the new theme.
					Config.Theme = val;
					ThemeManager.SelectTheme(val);
				},
				name: () => Helper.Translation.Get("theme.select"),

				// Now, for allowed values and formatting them. The allowed
				// values are all the keys of our dictionary, so just use
				// those here.
				allowedValues: choices.Keys.ToArray(),

				// And for formatting, try to get the value from the choices
				// and use that. Easy.
				formatAllowedValue: val =>
				{
					if (choices.TryGetValue(val, out string name))
						return name;
					return val;
				}
			);
		}

		#endregion

	}
}
