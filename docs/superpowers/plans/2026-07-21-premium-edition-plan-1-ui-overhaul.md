# Premium Edition – Plan 1: UI/UX Overhaul

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Modernize System Sweep with Acrylic backdrop, Fluent icons, page animations, theme switching, hover effects, smooth scrollbar, and animated progress.

**Architecture:** WPF .NET 8 app using wpfui 3.1.1. All changes are in XAML styles, code-behind, and service layer. This is the UI foundation that Plans 2 and 3 build upon.

**Tech Stack:** WPF UI 3.1.1, Segoe Fluent Icons font, .NET 8.0-windows

## Global Constraints

- WPF-UI 3.1.1: Use `FluentWindow`, `WindowBackdropType`, standard `<Page>`
- All theme colors via `ui:ThemesDictionary` (no hardcoded `#FF...` where theming applies)
- Only use `#FF` colors for backgrounds that are NOT theme-dependent (card backgrounds, etc.)
- Animations: 200ms with QuadraticEase, no dependency on external animation libraries
- Icons: Use wpfui `SymbolRegular` / `Icon` property where possible, fallback to Unicode glyphs from Segoe Fluent Icons font

---

### Task 1: Acrylic Backdrop + Theme Infrastructure

**Files:**
- Modify: `cleaner1/MainWindow.xaml`
- Modify: `cleaner1/App.xaml`
- Modify: `cleaner1/AppSettings.cs`
- Create: `cleaner1/Services/ThemeService.cs`

**Interfaces:**
- Consumes: `AppSettings` (existing, adds `Theme` property)
- Produces: `ThemeService` (SetTheme(string), CurrentTheme), AppSettings.Theme property

- [ ] **Step 1: Add Theme property to AppSettings.cs**

Add `public string Theme { get; set; } = "Dark";` to AppSettings class. Wire into constructor (`Theme = "Dark"`), Load() and Save().

- [ ] **Step 2: Create ThemeService.cs**

```csharp
using System.Windows;
using Wpf.Ui.Appearance;

namespace ModernFileCleaner.Services;

public static class ThemeService
{
    public static string CurrentTheme { get; private set; } = "Dark";

    public static void SetTheme(string theme)
    {
        CurrentTheme = theme;
        ApplicationTheme appTheme = theme switch
        {
            "Light" => ApplicationTheme.Light,
            _ => ApplicationTheme.Dark
        };
        ThemeApply.SetTheme(appTheme);
    }

    public static void Toggle()
    {
        SetTheme(CurrentTheme == "Dark" ? "Light" : "Dark");
    }
}
```

- [ ] **Step 3: Update App.xaml – remove hardcoded Dark theme**

Remove `<ui:ThemesDictionary Theme="Dark" />` from App.xaml (theme is now applied programmatically by ThemeService). Keep only `ui:ControlsDictionary`.

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ui:ControlsDictionary />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

- [ ] **Step 4: Update MainWindow.xaml – Acrylic + remove hardcoded theme**

Change `WindowBackdropType="Mica"` to `WindowBackdropType="Acrylic"`. Remove the `<ui:FluentWindow.Resources>` block with ThemesDictionary (theme is now from ThemeService).

```xml
<ui:FluentWindow x:Class="ModernFileCleaner.MainWindow"
                  ...
                  WindowBackdropType="Acrylic">
    <!-- NO Resources block here -->
    <Grid>
        ...
    </Grid>
</ui:FluentWindow>
```

- [ ] **Step 5: Wire ThemeService in App.xaml.cs OnStartup**

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    AppSettings.Instance.Load();
    ThemeService.SetTheme(AppSettings.Instance.Theme);
}
```

- [ ] **Step 6: Build**

Run: `dotnet build cleaner1/cleaner1.csproj`
Expected: 0 errors, 0 warnings

- [ ] **Step 7: Commit**

```bash
git add cleaner1/App.xaml cleaner1/App.xaml.cs cleaner1/AppSettings.cs cleaner1/MainWindow.xaml cleaner1/Services/ThemeService.cs
git commit -m "feat: add Acrylic backdrop, ThemeService, and theme setting"
```

---

### Task 2: Add Theme Toggle to NavigationView Footer

**Files:**
- Modify: `cleaner1/MainWindow.xaml`
- Modify: `cleaner1/MainWindow.xaml.cs`

- [ ] **Step 1: Add footer toggle in MainWindow.xaml**

Add a `NavigationView.FooterMenuItems` with a Theme toggle button:

```xml
<ui:NavigationView>
    <ui:NavigationView.MenuItems>
        ...
    </ui:NavigationView.MenuItems>
    <ui:NavigationView.FooterMenuItems>
        <ui:NavigationViewItem x:Name="navTheme" Content="Dark Mode" Icon="DarkTheme24" Tag="theme" />
    </ui:NavigationView.FooterMenuItems>
</ui:NavigationView>
```

- [ ] **Step 2: Handle theme toggle in MainWindow.xaml.cs**

Add to OnSelectionChanged:
```csharp
case "theme":
    ThemeService.Toggle();
    navTheme.Content = ThemeService.CurrentTheme == "Dark" ? "Dark Mode" : "Light Mode";
    navTheme.Icon = ThemeService.CurrentTheme == "Dark" ? "DarkTheme24" : "LightTheme24";
    // Reset selection to previous page
    NavView.SelectedItem = NavView.MenuItems[0];
    break;
```

Also persist theme choice:
```csharp
case "theme":
    ThemeService.Toggle();
    AppSettings.Instance.Theme = ThemeService.CurrentTheme;
    AppSettings.Instance.Save();
    navTheme.Content = ThemeService.CurrentTheme == "Dark" ? "Dark Mode" : "Light Mode";
    navTheme.Icon = ThemeService.CurrentTheme == "Dark" ? "DarkTheme24" : "LightTheme24";
    NavView.SelectedItem = NavView.MenuItems[0];
    break;
```

- [ ] **Step 3: Build**

Run: `dotnet build cleaner1/cleaner1.csproj`

- [ ] **Step 4: Commit**

```bash
git add cleaner1/MainWindow.xaml cleaner1/MainWindow.xaml.cs
git commit -m "feat: add Dark/Light theme toggle in NavigationView footer"
```

---

### Task 3: Replace Emoji Icons with Fluent Icons

**Files:**
- Modify: `cleaner1/Pages/CleanPage.xaml`
- Modify: `cleaner1/Pages/DashboardPage.xaml` (if exists, else skip)
- Modify: `cleaner1/Pages/StatsPage.xaml`
- Modify: `cleaner1/Pages/SettingsPage.xaml`
- Modify: `cleaner1/Pages/AboutPage.xaml`
- Create: `cleaner1/Services/IconService.cs`

- [ ] **Step 1: Create IconService.cs**

```csharp
namespace ModernFileCleaner.Services;

public static class IconService
{
    public static string GetIcon(string categoryId) => categoryId switch
    {
        "temp_files" => "",      // Delete
        "recycle_bin" => "",     // RecycleBin
        "download_cache" => "",  // Download
        "thumbnail_cache" => "", // Pictures
        "error_reports" => "",   // Warning
        "installer_temp" => "",  // Package
        "store_cache" => "",     // StoreLogo
        "windows_logs" => "",    // Document
        "windows_old" => "",     // Windows
        "memory_dumps" => "",    // Memory
        _ => ""
    };
}
```

- [ ] **Step 2: Add Font resource to App.xaml**

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ui:ControlsDictionary />
        </ResourceDictionary.MergedDictionaries>
        <FontFamily x:Key="FluentIcons">/Wpf.Ui;Component/Resources/Font/#Segore Fluent Icons</FontFamily>
    </ResourceDictionary>
</Application.Resources>
```

Actually, wpfui already uses FontIcon with `Icon` property on NavigationViewItem. For regular TextBlock, use `Segoe Fluent Icons` font family.

Replace emoji icons in XAML headers:
```xml
<!-- Before: -->
<TextBlock Text="🧹" FontSize="28" .../>

<!-- After: -->
<TextBlock Text="&#xE8B7;" FontFamily="/Wpf.Ui;Component/Resources/Font/#Segoe Fluent Icons" FontSize="24" .../>
```

- [ ] **Step 3: Update CleanPage.xaml header icon**

Replace `Text="🧹"` with `Text="&#xE8B7;"` (Delete icon) and add `FontFamily`.

- [ ] **Step 4: Update StatsPage.xaml header icon**

Replace `Text="📊"` with `Text="&#xE933;"` (DataBarVertical) + FontFamily.

- [ ] **Step 5: Update SettingsPage.xaml header icon**

Replace `Text="⚙️"` with `Text="&#xE713;"` (Settings) + FontFamily.

- [ ] **Step 6: Update AboutPage.xaml app icon**

Replace `Text="🧹"` in the icon circle with a Fluent icon `Text="&#xE8B7;"` + FontFamily.

- [ ] **Step 7: Build**

Run: `dotnet build cleaner1/cleaner1.csproj`

- [ ] **Step 8: Commit**

```bash
git add cleaner1/Services/IconService.cs cleaner1/Pages/*.xaml
git commit -m "feat: replace emoji icons with Segoe Fluent Icons"
```

---

### Task 4: Page Transition Animations

**Files:**
- Create: `cleaner1/Styles/Transitions.xaml`
- Create: `cleaner1/Services/TransitionService.cs`
- Modify: `cleaner1/MainWindow.xaml`
- Modify: `cleaner1/MainWindow.xaml.cs`

- [ ] **Step 1: Create Transitions.xaml (resource dictionary)**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <Storyboard x:Key="PageFadeIn">
        <DoubleAnimation Storyboard.TargetProperty="Opacity"
                         From="0" To="1"
                         Duration="0:0:0.2"
                         EasingFunction="{StaticResource FadeEasing}"/>
    </Storyboard>
    
    <PowerEase x:Key="FadeEasing" EasingMode="EaseOut" Power="2"/>
</ResourceDictionary>
```

- [ ] **Step 2: Load Transitions.xaml in App.xaml**

```xml
<ResourceDictionary.MergedDictionaries>
    <ui:ControlsDictionary />
    <ResourceDictionary Source="Styles/Transitions.xaml"/>
</ResourceDictionary.MergedDictionaries>
```

- [ ] **Step 3: Add fade transition to Frame navigation in MainWindow.xaml.cs**

Add `Frame.Navigated` event handler:
```csharp
NavFrame.Navigated += (_, _) =>
{
    if (NavFrame.Content is FrameworkElement element)
    {
        element.Opacity = 0;
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            From = 0, To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }
};
```

- [ ] **Step 4: Build**

Run: `dotnet build cleaner1/cleaner1.csproj`

- [ ] **Step 5: Commit**

```bash
git add cleaner1/Styles/Transitions.xaml cleaner1/App.xaml cleaner1/MainWindow.xaml.cs
git commit -m "feat: add page fade transition animations"
```

---

### Task 5: Card Hover Effects

**Files:**
- Modify: `cleaner1/Controls/CleaningCard.xaml`

- [ ] **Step 1: Add hover effects to CleaningCard.xaml**

Add a `RenderTransform` (ScaleTransform) with center at 0.5,0.5 and trigger on IsMouseOver:

```xml
<Border.RenderTransform>
    <ScaleTransform CenterX="0.5" CenterY="0.5" ScaleX="1" ScaleY="1"/>
</Border.RenderTransform>
<Border.Resources>
    <Storyboard x:Key="HoverIn">
        <DoubleAnimation To="1.02" Duration="0:0:0.15" 
                         Storyboard.TargetProperty="RenderTransform.ScaleX"
                         EasingFunction="{StaticResource HoverEasing}"/>
        <DoubleAnimation To="1.02" Duration="0:0:0.15"
                         Storyboard.TargetProperty="RenderTransform.ScaleY"
                         EasingFunction="{StaticResource HoverEasing}"/>
    </Storyboard>
    <Storyboard x:Key="HoverOut">
        <DoubleAnimation To="1.0" Duration="0:0:0.15"
                         Storyboard.TargetProperty="RenderTransform.ScaleX"
                         EasingFunction="{StaticResource HoverEasing}"/>
        <DoubleAnimation To="1.0" Duration="0:0:0.15"
                         Storyboard.TargetProperty="RenderTransform.ScaleY"
                         EasingFunction="{StaticResource HoverEasing}"/>
    </Storyboard>
</Border.Resources>
<Border.Style>
    <Style TargetType="Border">
        <Setter Property="BorderBrush" Value="#FF3D3D3D"/>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Trigger.EnterActions>
                    <BeginStoryboard Storyboard="{StaticResource HoverIn}"/>
                </Trigger.EnterActions>
                <Trigger.ExitActions>
                    <BeginStoryboard Storyboard="{StaticResource HoverOut}"/>
                </Trigger.ExitActions>
            </Trigger>
            <!-- Safety DataTriggers remain -->
        </Style.Triggers>
    </Style>
</Border.Style>
```

Add easing to Transitions.xaml:
```xml
<PowerEase x:Key="HoverEasing" EasingMode="EaseOut" Power="3"/>
```

- [ ] **Step 2: Build**

Run: `dotnet build cleaner1/cleaner1.csproj`

- [ ] **Step 3: Commit**

```bash
git add cleaner1/Controls/CleaningCard.xaml cleaner1/Styles/Transitions.xaml
git commit -m "feat: add card hover scale animation"
```

---

### Task 6: Modern ScrollBar Style

**Files:**
- Create: `cleaner1/Styles/ModernScrollBar.xaml`
- Modify: `cleaner1/App.xaml`

- [ ] **Step 1: Create ModernScrollBar.xaml**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <Style x:Key="ThinScrollBar" TargetType="ScrollBar">
        <Setter Property="Width" Value="6"/>
        <Setter Property="Margin" Value="0"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="ScrollBar">
                    <Track Name="PART_Track" IsDirectionReversed="True">
                        <Track.Thumb>
                            <Thumb BorderThickness="0" Height="Auto">
                                <Thumb.Style>
                                    <Style TargetType="Thumb">
                                        <Setter Property="Template">
                                            <Setter.Value>
                                                <ControlTemplate>
                                                    <Border CornerRadius="3" 
                                                            Background="#FF555555" 
                                                            Margin="0,0,0,0"/>
                                                </ControlTemplate>
                                            </Setter.Value>
                                        </Setter>
                                    </Style>
                                </Thumb.Style>
                            </Thumb>
                        </Track.Thumb>
                    </Track>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
```

- [ ] **Step 2: Load in App.xaml**

```xml
<ResourceDictionary Source="Styles/ModernScrollBar.xaml"/>
```

- [ ] **Step 3: Apply to ScrollViewers**

In CleanPage.xaml, add `Style="{StaticResource ThinScrollBar}"` to the ScrollViewer. Or better, make it global by applying in App.xaml:
```xml
<Style TargetType="ScrollViewer">
    <Setter Property="PanningMode" Value="Both"/>
</Style>
```

- [ ] **Step 4: Build**

- [ ] **Step 5: Commit**

```bash
git add cleaner1/Styles/ModernScrollBar.xaml cleaner1/App.xaml
git commit -m "feat: add modern thin scrollbar style"
```

---

### Task 7: Smooth ProgressBar Animation

**Files:**
- Create: `cleaner1/Styles/ProgressStyles.xaml`
- Modify: `cleaner1/Pages/CleanPage.xaml`

- [ ] **Step 1: Create ProgressStyles.xaml with animated progress bar**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <Style x:Key="SmoothProgressBar" TargetType="ProgressBar">
        <Setter Property="Height" Value="6"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Background" Value="#FF3D3D3D"/>
        <Setter Property="Foreground" Value="#FF0078D4"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="ProgressBar">
                    <Border Background="{TemplateBinding Background}" 
                            CornerRadius="3">
                        <Border x:Name="PART_Track" 
                                Background="{TemplateBinding Foreground}"
                                CornerRadius="3"
                                HorizontalAlignment="Left"/>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
```

- [ ] **Step 2: Load in App.xaml**

- [ ] **Step 3: Apply style in CleanPage.xaml**

```xml
<ProgressBar x:Name="ProgressBar" Style="{StaticResource SmoothProgressBar}" .../>
```

- [ ] **Step 4: Build**

- [ ] **Step 5: Commit**

```bash
git add cleaner1/Styles/ProgressStyles.xaml cleaner1/App.xaml
git commit -m "feat: add smooth progress bar style"
```
