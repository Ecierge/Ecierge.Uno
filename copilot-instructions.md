# Project details
F# 10
C# 14
.NET 10
Nullability checks enabled

## Libraries we use

If you need any source code you can find it in the following repositories:
* [CommunityToolkit.WinUI](https://github.com/CommunityToolkit/Windows) `main` branch
* [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) `main` branch
* [Microsoft.Windows.SDK](https://github.com/microsoft/windowsappsdk) `main` branch
* [SkiaSharp](https://github.com/mono/SkiaSharp) `main` branch
* [OpenIddict](https://github.com/openiddict/openiddict-core) `dev` branch
* [Uno](https://github.com/unoplatform/uno) `master` branch
* [Uno.Extensions](https://github.com/unoplatform/uno.extensions) `main` branch
* [Uno.Themes](https://github.com/unoplatform/Uno.Themes) `master` branch
* [Uno.Toolkit.WinUI](https://github.com/unoplatform/uno.toolkit.ui) `main` branch

# How to create custom controls

During the implementation of custom control, you should consider the following steps:
1. **Define the Control Class**: Create a new class that inherits from an appropriate base control class (e.g., `Control`, `UserControl`, etc.).
2. **Create .xaml file**: If your control has a visual representation, create a corresponding `.xaml` file to define the layout and appearance of the control in the same directory.
3. **Link the created .xaml file as a resource in the Generic.xaml file**
