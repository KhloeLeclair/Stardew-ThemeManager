# ThemeManager

> **Hey!** This is for SMAPI v3.14 / v4.00 or later, when the new asset loading pipeline was introduced. If you're looking for SMAPI v3.13 and lower support, check the [main-3 branch](https://github.com/KhloeLeclair/Stardew-ThemeManager/tree/main-3).

* [What is ThemeManager?](#what-is-thememanager)
* [Quick! How do I use this?](#quick-how-do-i-use-this)
* [So, what is a Theme?](#so-what-is-a-theme)
* [What does the C# look like?](#what-does-the-c-look-like)
* [Well, what does a `theme.json` look like?](#well-what-does-a-themejson-file-look-like)
* [Content Patcher Integration](#content-patcher-integration)
* [Theme Folder Structure](#theme-folder-structure)
* [Changelog](CHANGELOG.md)

## What is ThemeManager?

ThemeManager is a content loader for Stardew Valley C# mods that:

1. Discovers available themes from three sources:
   * The current mod's `assets/themes/` folder.
   * Content Packs for the current mod that have a `theme.json` file.
   * Other mods that have a `my-mod.unique-id:theme` key in their manifest, pointing to a theme json file.

2. Selects an appropriate theme out of all available themes based on which mods are installed.

3. Makes it easy to let users choose a theme with a config file.

4. Makes it easy to reload themes at runtime for easier development.

5. Passes resources through SMAPI's AssetRequested event so that Content Patcher packs can modify resources.

6. Exists in a single file that is easy to drop into a project with no other dependencies.

Also, ThemeManager is licensed under MIT-0. You can use this. You don't have to credit me. Please use it. Make life easier for people making content packs.


## Quick! How do I use this?

1. Grab the `ThemeManager.cs` (and optionally `ColorConverter.cs`) file from this repository and drop it in your project. Everything is neat and self contained. In the future, if there's an update, just replace the file with the updated one.
2. Add a `ThemeManager` instance to your ModEntry (or somewhere else I guess, if you want).
3. Construct that `ThemeManager` and call its `.Discover()` method to populate the list of available themes and load one.
4. Use `ThemeManager.Load<T>(path)` instead of `Helper.Content.Load<T>(path)` for theme-aware asset loading, as appropriate.

Optionally:

5. Register a console command for changing the current theme.
6. Register a theme picker in Generic Mod Config Menu.
7. Listen to the `ThemeChanged` event and do anything you need to reload assets from your theme.
8. Subclass `BaseThemeData` and add extra colors and other values for themes to override, making themes more functional.

The included example mod does all of that, so check its `ModEntry` and see for yourself.


## So, what is a theme?

Themes are a mix of custom data, which is loaded from a `theme.json` file, as well as textures and potentially other resources loaded from `assets/`. Mod authors can support as much or as little extra data as they want.


## What does the C# look like?

You're looking at the example project! If you don't want to check our ModEntry.cs file, here's the basics!

```csharp
using Leclair.Stardew.ThemeManager;

namespace MyCoolMod {

	// All theme data types extend BaseThemeData,
	// which itself only contains a couple very
	// basic properties to do with loading / enumeration
	// and nothing to do with appearance.
	class MyThemeData : BaseThemeData {

		int PaddingTop { get; set; } = 8;

		Color? TextColor { get; set; }
	}

	// Just your everyday, average config file.
	class MyConfig {
		string Theme { get; set; } = "automatic";
	}

	// The actual Mod entry point.
	class ModEntry : Mod {
		internal MyConfig Config;

		// When declaring your ThemeManager, you provide
		// your own theme data class (or use BaseThemeData).
		internal ThemeManager<MyThemeData> ThemeManager;

		// I find it very useful to set up a property for
		// quick access to the current theme
		internal MyThemeData Theme => ThemeManager.Theme;

		public override void Entry(IModHelper helper) {
			// Init stuff
			Config = Helper.ReadConfig<MyConfig>();

			// Theme Manager
			ThemeManager = new(this, Config.Theme);
			ThemeManager.ThemeChanged += OnThemeChanged;
			ThemeManager.Discover();
		}

		private void OnThemeChanged(object sender, ThemeChangedEventArgs<MyThemeData> e) {
			// Do stuff when the user changes theme, like
			// reload textures.
		}

		private void SomewhereElse() {
			// Instead of
			Helper.Content.Load<Texture2D>("blah.png");

			// you do
			ThemeManager.Load<Texture2D>("blah.png");

			// And also stuff like
			Color text = Theme?.TextColor ?? Game1.textColor;
		}
	}

}
```

> Notice how we use `Theme?.TextColor`, because the theme might be null. You can also provide a default value for your theme which prevents that.


## Well, what does a `theme.json` file look like?

Glad you asked. For the above, hypothetical mod:
```js
{
	// This property is only used when loading a theme from your mod's
	// assets/themes/ folder. Otherwise, it is ignored. This is for
	// setting a default, human readable name for your theme.
	"Name": "Flower",

	// For displaying a list of themes in the Generic Mod Config Menu,
	// you can provide localized names of your theme. Please note that
	// this is optional. We're not using the i18n system because it's
	// honestly complete overkill when these are the only translated
	// strings provided by themes.
	"LocalizedNames": {
		"es": "La Flor"
	},

	// A list of unique IDs of mods that this theme is for compatibility
	// with. If a mod in this list is loaded, this theme will be selected
	// when the current theme is set to automatic.
	"For": [
		"SomeOtherRetextureMod.UniqueId"
	],

	// If you keep your assets for this theme in a different folder than
	// "assets/", you can override that here. This can otherwise be
	// left out from your theme.
	"AssetPrefix": "assets",

	// And... that's it. Everything above is optional, and there's
	// nothing else in BaseThemeData. For the earlier C# example though,
	// we also have a color. So...
	"TextColor": "#222"
}
```


## Content Patcher Integration

When Content Patcher is loaded, ThemeManager will redirect `.Load<>()` requests through GameContent using its implementation of `IAssetLoader`.

Say, for example, you request a menu texture:
```csharp
var texture = ThemeManager.Load<Texture2D>("Menu.png");
```

Without Content Patcher, we just load the file directly and return it. Simple. Efficient. But less flexible.

When Content Patcher is present, we instead pass that call to something like:
```csharp
Helper.Content.Load<Texture2D>("Mods/MyName.MyCoolMod/Themes/SomeoneElse.TheirThemesName/Menu.png", ContentSource.GameContent);
```

To break it down, that string is combined from:

1. The literal string `Mods/`
2. Your mod's unique ID
3. The literal string `/Themes/`
4. The current theme's unique ID
5. The literal string `/`
6. The requested asset path.

Our event handler for `AssetRequested` is in charge of intercepting that request later on, and actually loading the base resource. The important thing, however, is that you can then in Content Patcher do something like
```json
{
	"Format": "1.25.0",
	"Changes": [
		{
			"Action": "EditImage",
			"Target": "Mods/MyName.MyCoolMod/Themes/SomeoneElse.TheirThemesName/Menu.png",
			"FromFile": "assets/MyButton.png",
			"ToArea": { "X": 160, "Y": 80, "Width": 16, "Height" 16 }
		}
	]
}
```

This way, content packs don't need to replace your entire asset when they don't need to, potentially improving future compatibility.


## Theme Folder Structure

As noted at the beginning, there are several ways that themes can be loaded.


### Your Mod's `assets/themes/` Folder
```
📁 Mods/
   📁 MyCoolMod/
      🗎 MyCoolMod.dll
      🗎 manifest.json
      📁 assets/
         📁 themes/
            📁 SomeTheme/
               🗎 theme.json
               📁 assets/
                  🗎 example.png
```


### Content Packs for Your Mod
```
📁 Mods/
   📁 [MCM] My Cool Theme/
      🗎 manifest.json
      🗎 theme.json
      📁 assets/
         🗎 example.png
```


### Other Mods

Including a theme in other mods is slightly more involved than just throwing down a `theme.json` file. How would we know that that specific file is for us? To make things explicit, we check the `manifest.json` of every mod for a matching theme key. For example:
```js
{
	// The Usual Suspects
	"Name": "Some Other Cool Mod",
	"Author": "A Super Cool Person",
	"Version": "5.4.3",
	"Description": "Totally rad stuff.",
	"UniqueID": "SuperCoolPerson.OtherCoolMod",
	"MinimumApiVersion": "3.7.3",
	"ContentPackFor": {
		"UniqueID": "Pathoschild.ContentPatcher"
	},

	// Our Theme!
	"MyName.MyCoolMod:theme": "compat/MyCoolMod/theme.json"
}
```

ThemeManager looks for a key starting with your mod's unique ID that then ends with `:theme`, and tries loading the theme JSON file it points to. If it succeeds, then it adds that theme using the folder the theme JSON file is in as the root folder of the theme. So, given the above, you'd end up with something like:

```
📁 Mods/
   📁 SomeOtherCoolMod/
      🗎 manifest.json
      📁 compat/
         📁 MyCoolMod/
            🗎 theme.json
            📁 assets/
               🗎 example.png
```