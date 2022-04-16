# ThemeManager

This branch of `ThemeManager` is deprecated and should not be used. It remains
in the repository only for the sake of preserving history.

Please use the [main-4 branch](https://github.com/KhloeLeclair/Stardew-ThemeManager/tree/main-4),
which is the default branch of this repository.

> **Q:** But doesn't the `main-4` branch only support SMAPI v3.14+
> and newer?

Nope! There's pre-processor directives in the file now that switch between
`AssetRequested` and `IAssetLoader`. If you're still developing for SMAPI
v3.13 or below, just uncomment this line at the top of the file:
```csharp
// #define THEME_MANAGER_PRE_314
```
