# Changelog

# Changelog

## 1.2.1
Released April 7th, 2022.

* Added a `ThemeManager.HasTheme(...)` method for determining if a specific
  theme is present and loaded.
* Updated the legacy `IAssetLoader` based version to include the new 1.2.0
  and 1.2.1 features.


## 1.2.0
Released April 7th, 2022.

### API

* Added a `PreventRedirection` option to `BaseThemeData` so that themes
  can opt out of asset redirection if they really want to, preventing
  interaction with Content Patcher.
* Added a `ThemeManager.GetTheme(themeId)` method for getting theme data
  for a theme other than the active theme. This may be useful if a
  specific part of your user interface should be displayed with a specific
  theme while other parts use the main theme.
* `ThemeManager.SelectTheme(...)` now has an optional `postReload` parameter
  that forces `ThemeManager` to purge any cached resources and always emit an
  event. This is used internally when `ThemeManager.Discover()` is called and
  all themes are reloaded to ensure that everything is up to date.
* `ThemeManager.Load<T>(...)` now has an optional `themeId` parameter that
  causes the requested resource to be loaded from a specific theme rather than
  the currently active theme.
* `ThemeManager.HasFile(...)` now has an optional `themeId` parameter that
  causes the check to be performed against a specific theme rather than the
  currently active theme.

### Fixes

* Lazy-load assets when using asset redirection. It's possible the assets
  may be cached already, so loading an extra copy just wastes resources.
* Fix a null reference exception in the case `ActiveThemeId` ever becomes
  set while there is no theme data for the active theme. I don't think
  this was possible, but there's no harm in making sure.

### Maintenance

* Annotate the file with `#nullable enable` and start using explicitly
  nullable values everywhere. 
* Use a custom record rather than `Tuple<>`. I just think it looks better.


## 1.1.1
Released April 3rd, 2022.

* Fix support for `AssetRequested` event by adding a priority argument.
* Use the `GameContent` and `ModContent` helpers rather than `Content`.

## 1.1.0
Released March 21st, 2022.

* Improve documentation.
* Move `IAssetLoader` into a subclass to minimize the API surface of
  the `ThemeManager` class.
* Add an `I18nPrefix` property for changing the auto-generated I18n keys.

## 1.0.0
Released March 20th, 2022.

* Initial release.
