using System;
using System.Windows;
using System.Windows.Media;

namespace RestaurantWorkApp
{
    public static class ThemeManager
    {
        private static bool _isDarkTheme = false;

        public static bool IsDarkTheme => _isDarkTheme;

        public static event EventHandler ThemeChanged;

        public static void ToggleTheme()
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme();
        }

        public static void SetDarkTheme(bool isDark)
        {
            if (_isDarkTheme != isDark)
            {
                _isDarkTheme = isDark;
                ApplyTheme();
            }
        }

        private static void ApplyTheme()
        {
            var app = Application.Current;
            if (app == null) return;

            // Светлая тема (по умолчанию)
            if (!_isDarkTheme)
            {
                // Основные цвета
                app.Resources["PrimaryColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x6B, 0x6B, 0x6B));
                app.Resources["PrimaryHoverColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x52, 0x52, 0x52));
                app.Resources["SecondaryColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x4E, 0xCD, 0xC4));
                app.Resources["DarkColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x2D, 0x34, 0x36));
                app.Resources["LightColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xF8, 0xF9, 0xFA));
                app.Resources["BackgroundColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xF5, 0xF5, 0xF5));
                app.Resources["WhiteColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
                app.Resources["GrayColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x63, 0x6E, 0x72));
                app.Resources["LightGrayColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xDF, 0xE6, 0xE9));
                app.Resources["SuccessColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xB8, 0x94));
                app.Resources["WarningColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFD, 0xCB, 0x6E));
                app.Resources["ErrorColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x76, 0x75));

                // Градиенты
                var primaryGradient = new LinearGradientBrush();
                primaryGradient.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x6B, 0x6B, 0x6B), 0));
                primaryGradient.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x52, 0x52, 0x52), 1));
                app.Resources["PrimaryGradient"] = primaryGradient;

                var secondaryGradient = new LinearGradientBrush();
                secondaryGradient.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x4E, 0xCD, 0xC4), 0));
                secondaryGradient.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x44, 0xBD, 0xB5), 1));
                app.Resources["SecondaryGradient"] = secondaryGradient;

                // Тени
                app.Resources["CardShadow"] = CreateShadow(Color.FromArgb(0xFF, 0x00, 0x00, 0x00), 0.08, 12, 2);
                app.Resources["HoverShadow"] = CreateShadow(Color.FromArgb(0xFF, 0x00, 0x00, 0x00), 0.12, 24, 8);
            }
            else
            {
                // Тёмная тема
                app.Resources["PrimaryColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x87, 0x87));
                app.Resources["PrimaryHoverColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x6B, 0x6B));
                app.Resources["SecondaryColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x6E, 0xE7, 0xE0));
                app.Resources["DarkColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xEA, 0xEA, 0xEA));
                app.Resources["LightColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x2A, 0x2A, 0x3E));
                app.Resources["BackgroundColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x1A, 0x1A, 0x2E));
                app.Resources["WhiteColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x16, 0x21, 0x3E));
                app.Resources["GrayColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xA0, 0xA0, 0xA0));
                app.Resources["LightGrayColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x4A, 0x4A, 0x6A));
                app.Resources["SuccessColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xD9, 0xA5));
                app.Resources["WarningColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xE0, 0x82));
                app.Resources["ErrorColor"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x8A, 0x80));

                // Градиенты
                var primaryGradient = new LinearGradientBrush();
                primaryGradient.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0x87, 0x87), 0));
                primaryGradient.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0x6B, 0x6B), 1));
                app.Resources["PrimaryGradient"] = primaryGradient;

                var secondaryGradient = new LinearGradientBrush();
                secondaryGradient.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x6E, 0xE7, 0xE0), 0));
                secondaryGradient.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x5D, 0xD4, 0xC9), 1));
                app.Resources["SecondaryGradient"] = secondaryGradient;

                // Тени
                app.Resources["CardShadow"] = CreateShadow(Color.FromArgb(0xFF, 0x00, 0x00, 0x00), 0.3, 16, 4);
                app.Resources["HoverShadow"] = CreateShadow(Color.FromArgb(0xFF, 0x00, 0x00, 0x00), 0.5, 32, 12);
            }

            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        private static DropShadowEffect CreateShadow(Color color, double opacity, double blurRadius, double shadowDepth)
        {
            return new DropShadowEffect
            {
                Color = color,
                Opacity = opacity,
                BlurRadius = blurRadius,
                ShadowDepth = shadowDepth,
                Direction = 270
            };
        }
    }
}
