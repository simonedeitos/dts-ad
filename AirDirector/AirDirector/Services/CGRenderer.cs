using AirDirector.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace AirDirector.Services
{
    public static class CGRenderer
    {
        // ═══════════════════════════════════════════════════════════
        // LOWER THIRD SETTINGS
        // ═══════════════════════════════════════════════════════════
        private static bool _lowerThirdEnabled = true;
        private static string _lowerThirdPosition = "BottomLeft";
        private static string _lowerThirdAnimation = "SlideLeft";
        private static int _lowerThirdEntranceTheme = 1;
        private static int _lowerThirdDelayStart = 2;
        private static int _lowerThirdDuration = 8;
        private static bool _lowerThirdShowAtEnd = false;
        private static int _lowerThirdEndOffset = 10;
        private static int _lowerThirdEndDuration = 5;
        private static string _lowerThirdLayout = "SingleLine";
        private static Color _lowerThirdBgColor = Color.FromArgb(200, 0, 0, 0);
        private static Color _lowerThirdTextColor = Color.White;
        private static Color _lowerThirdAccentColor = Color.FromArgb(255, 200, 0);
        private static string _lowerThirdFontFamily = "Segoe UI";
        private static int _lowerThirdTitleFontSize = 26;
        private static int _lowerThirdArtistFontSize = 20;
        private static int _lowerThirdMarginX = 50;
        private static int _lowerThirdMarginY = 50;

        // ═══════════════════════════════════════════════════════════
        // PERSISTENT INFO BAR SETTINGS
        // ═══════════════════════════════════════════════════════════
        private static bool _persistentInfoEnabled = true;
        private static Color _progressBarColor = Color.FromArgb(255, 50, 50);
        private static int _persistentInfoHideBeforeEnd = 15;
        private static int _persistentInfoMarginX = 20;
        private static int _persistentInfoMarginY = 20;
        private static int _persistentInfoFontSize = 14;

        // ═══════════════════════════════════════════════════════════
        // LOGO SETTINGS
        // ═══════════════════════════════════════════════════════════
        private static bool _logoEnabled = true;
        private static string _logoPath = "";
        private static string _logoPosition = "TopRight";
        private static int _logoOpacity = 100;
        private static int _logoSize = 150;
        private static int _logoMargin = 20;
        private static Bitmap _logoBitmap = null;
        private static List<AdditionalLogo> _additionalLogos = new List<AdditionalLogo>();
        private static HashSet<string> _visibleAdditionalLogos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ═══════════════════════════════════════════════════════════
        // CLOCK SETTINGS
        // ═══════════════════════════════════════════════════════════
        private static bool _clockEnabled = true;
        private static bool _clockUnderLogo = true;
        private static Color _clockColor = Color.White;
        private static Color _clockBgColor = Color.FromArgb(150, 0, 0, 0);
        private static int _clockFontSize = 18;
        private static bool _clockBgEnabled = true;

        // ═══════════════════════════════════════════════════════════
        // SPOT/ADV LABEL SETTINGS
        // ═══════════════════════════════════════════════════════════
        private static bool _spotLabelEnabled = true;
        private static string _spotLabelText = "ADVERTISING";
        private static string _spotLabelPosition = "TopLeft";
        private static Color _spotLabelBgColor = Color.FromArgb(200, 255, 0, 0);
        private static Color _spotLabelTextColor = Color.White;
        private static bool _spotLabelBgEnabled = true;
        private static int _spotLabelFontSize = 14;
        private static int _spotLabelMarginX = 20;
        private static int _spotLabelMarginY = 20;

        // ═══════════════════════════════════════════════════════════
        // CURRENT STATE
        // ═══════════════════════════════════════════════════════════
        private static string _currentArtist = "";
        private static string _currentTitle = "";
        private static string _currentItemType = "Music";
        private static DateTime _trackStartTime = DateTime.MinValue;
        private static TimeSpan _trackDuration = TimeSpan.Zero;
        private static bool _settingsLoaded = false;

        // Animation state
        private static float _animProgress = 0f;
        private static float _animOutProgress = 0f;
        private static bool _isAnimatingIn = false;
        private static bool _isAnimatingOut = false;
        private static bool _isShowingLowerThird = false;
        private static bool _hasShownInitialTitle = false;
        private static DateTime _animStartTime = DateTime.MinValue;
        private static DateTime _animOutStartTime = DateTime.MinValue;

        private const float ANIM_DURATION_SEC = 0.5f;

        // ═══════════════════════════════════════════════════════════
        // INITIALIZATION
        // ═══════════════════════════════════════════════════════════
        static CGRenderer()
        {
            LoadSettings();
        }

        public static void ReloadSettings()
        {
            LoadSettings();
            LoadLogo();
        }

        public static void ShowAdditionalLogo(string logoImagePath)
        {
            if (string.IsNullOrWhiteSpace(logoImagePath))
                return;

            _visibleAdditionalLogos.Add(logoImagePath.Trim());
            SaveAdditionalLogoVisibility();
        }

        public static void HideAdditionalLogo(string logoImagePath)
        {
            if (string.IsNullOrWhiteSpace(logoImagePath))
                return;

            _visibleAdditionalLogos.Remove(logoImagePath.Trim());
            SaveAdditionalLogoVisibility();
        }

        private static void LoadSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector\CG", false))
                {
                    if (key == null)
                    {
                        _settingsLoaded = true;
                        return;
                    }

                    // Lower Third
                    _lowerThirdEnabled = GetRegInt(key, "LowerThirdEnabled", 1) == 1;
                    _lowerThirdPosition = GetRegString(key, "LowerThirdPosition", "BottomLeft");
                    _lowerThirdAnimation = GetRegString(key, "LowerThirdAnimation", "SlideLeft");
                    _lowerThirdEntranceTheme = GetRegInt(key, "LowerThirdEntranceTheme", 1);
                    _lowerThirdDelayStart = GetRegInt(key, "LowerThirdDelayStart", 2);
                    _lowerThirdDuration = GetRegInt(key, "LowerThirdDuration", 8);
                    _lowerThirdShowAtEnd = GetRegInt(key, "LowerThirdShowAtEnd", 0) == 1;
                    _lowerThirdEndOffset = GetRegInt(key, "LowerThirdEndOffset", 10);
                    _lowerThirdEndDuration = GetRegInt(key, "LowerThirdEndDuration", 5);
                    _lowerThirdLayout = GetRegString(key, "LowerThirdLayout", "SingleLine");
                    _lowerThirdBgColor = Color.FromArgb(GetRegInt(key, "LowerThirdBgColor", Color.FromArgb(200, 0, 0, 0).ToArgb()));
                    _lowerThirdTextColor = Color.FromArgb(GetRegInt(key, "LowerThirdTextColor", Color.White.ToArgb()));
                    _lowerThirdAccentColor = Color.FromArgb(GetRegInt(key, "LowerThirdAccentColor", Color.FromArgb(255, 200, 0).ToArgb()));
                    _lowerThirdFontFamily = GetRegString(key, "LowerThirdFontFamily", "Segoe UI");
                    _lowerThirdTitleFontSize = GetRegInt(key, "LowerThirdTitleFontSize", 26);
                    _lowerThirdArtistFontSize = GetRegInt(key, "LowerThirdArtistFontSize", 20);
                    _lowerThirdMarginX = GetRegInt(key, "LowerThirdMarginX", 50);
                    _lowerThirdMarginY = GetRegInt(key, "LowerThirdMarginY", 50);

                    // Persistent Info
                    _persistentInfoEnabled = GetRegInt(key, "PersistentInfoEnabled", 1) == 1;
                    _progressBarColor = Color.FromArgb(GetRegInt(key, "ProgressBarColor", Color.FromArgb(255, 50, 50).ToArgb()));
                    _persistentInfoHideBeforeEnd = GetRegInt(key, "PersistentInfoHideBeforeEnd", 15);
                    _persistentInfoMarginX = GetRegInt(key, "PersistentInfoMarginX", 20);
                    _persistentInfoMarginY = GetRegInt(key, "PersistentInfoMarginY", 20);
                    _persistentInfoFontSize = GetRegInt(key, "PersistentInfoFontSize", 14);

                    // Logo
                    _logoEnabled = GetRegInt(key, "LogoEnabled", 1) == 1;
                    _logoPath = GetRegString(key, "LogoPath", "");
                    _logoPosition = GetRegString(key, "LogoPosition", "TopRight");
                    _logoOpacity = GetRegInt(key, "LogoOpacity", 100);
                    _logoSize = GetRegInt(key, "LogoSize", 150);
                    _logoMargin = GetRegInt(key, "LogoMargin", 20);
                    _additionalLogos = ParseAdditionalLogos(GetRegString(key, "AdditionalLogosJson", "[]"));
                    _visibleAdditionalLogos = ParseAdditionalLogoPathSet(GetRegString(key, "AdditionalLogosVisible", ""));

                    if (_visibleAdditionalLogos.Count == 0)
                    {
                        var legacyHiddenAdditionalLogos = ParseAdditionalLogoPathSet(GetRegString(key, "AdditionalLogosHidden", ""));
                        if (legacyHiddenAdditionalLogos.Count > 0)
                        {
                            foreach (var logo in _additionalLogos)
                            {
                                if (logo == null || string.IsNullOrWhiteSpace(logo.ImagePath))
                                    continue;

                                var path = logo.ImagePath.Trim();
                                if (!legacyHiddenAdditionalLogos.Contains(path))
                                    _visibleAdditionalLogos.Add(path);
                            }
                        }
                    }

                    NormalizeVisibleAdditionalLogos();

                    // Clock
                    _clockEnabled = GetRegInt(key, "ClockEnabled", 1) == 1;
                    _clockUnderLogo = GetRegInt(key, "ClockUnderLogo", 1) == 1;
                    _clockColor = Color.FromArgb(GetRegInt(key, "ClockColor", Color.White.ToArgb()));
                    _clockBgColor = Color.FromArgb(GetRegInt(key, "ClockBgColor", Color.FromArgb(150, 0, 0, 0).ToArgb()));
                    _clockFontSize = GetRegInt(key, "ClockFontSize", 18);
                    _clockBgEnabled = GetRegInt(key, "ClockBgEnabled", 1) == 1;

                    // Spot Label
                    _spotLabelEnabled = GetRegInt(key, "SpotLabelEnabled", 1) == 1;
                    _spotLabelText = GetRegString(key, "SpotLabelText", "ADVERTISING");
                    _spotLabelPosition = GetRegString(key, "SpotLabelPosition", "TopLeft");
                    _spotLabelBgColor = Color.FromArgb(GetRegInt(key, "SpotLabelBgColor", Color.FromArgb(200, 255, 0, 0).ToArgb()));
                    _spotLabelTextColor = Color.FromArgb(GetRegInt(key, "SpotLabelTextColor", Color.White.ToArgb()));
                    _spotLabelBgEnabled = GetRegInt(key, "SpotLabelBgEnabled", 1) == 1;
                    _spotLabelFontSize = GetRegInt(key, "SpotLabelFontSize", 14);
                    _spotLabelMarginX = GetRegInt(key, "SpotLabelMarginX", 20);
                    _spotLabelMarginY = GetRegInt(key, "SpotLabelMarginY", 20);

                    _settingsLoaded = true;
                }

                LoadLogo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CGRenderer] Load error: {ex.Message}");
                _settingsLoaded = true;
            }
        }

        private static void LoadLogo()
        {
            try
            {
                _logoBitmap?.Dispose();
                _logoBitmap = null;

                if (!string.IsNullOrEmpty(_logoPath) && File.Exists(_logoPath))
                {
                    using (var img = Image.FromFile(_logoPath))
                    {
                        float ratio = (float)img.Height / img.Width;
                        int newW = _logoSize;
                        int newH = (int)(_logoSize * ratio);

                        _logoBitmap = new Bitmap(newW, newH);
                        using (Graphics g = Graphics.FromImage(_logoBitmap))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(img, 0, 0, newW, newH);
                        }
                    }
                    Console.WriteLine($"[CGRenderer] Logo loaded:  {Path.GetFileName(_logoPath)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CGRenderer] Logo load error: {ex.Message}");
            }
        }

        private static List<AdditionalLogo> ParseAdditionalLogos(string json)
        {
            try
            {
                var logos = JsonConvert.DeserializeObject<List<AdditionalLogo>>(json ?? "[]");
                return logos ?? new List<AdditionalLogo>();
            }
            catch
            {
                return new List<AdditionalLogo>();
            }
        }

        private static HashSet<string> ParseAdditionalLogoPathSet(string serialized)
        {
            return new HashSet<string>(
                (serialized ?? "")
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
        }

        private static void NormalizeVisibleAdditionalLogos()
        {
            var availablePaths = new HashSet<string>(
                _additionalLogos
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.ImagePath))
                    .Select(x => x.ImagePath.Trim()),
                StringComparer.OrdinalIgnoreCase);

            _visibleAdditionalLogos.RemoveWhere(x => !availablePaths.Contains(x));
        }

        private static void SaveAdditionalLogoVisibility()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AirDirector\CG"))
                {
                    key?.SetValue("AdditionalLogosVisible", string.Join(";", _visibleAdditionalLogos));
                }
            }
            catch { }
        }

        private static string GetRegString(RegistryKey key, string name, string def)
        {
            object val = key.GetValue(name);
            return val?.ToString() ?? def;
        }

        private static int GetRegInt(RegistryKey key, string name, int def)
        {
            object val = key.GetValue(name);
            if (val != null && int.TryParse(val.ToString(), out int result))
                return result;
            return def;
        }

        // ═══════════════════════════════════════════════════════════
        // TRACK CHANGED NOTIFICATION
        // ═══════════════════════════════════════════════════════════
        public static void OnTrackChanged(string artist, string title, string itemType, TimeSpan duration)
        {
            _currentArtist = artist ?? "";
            _currentTitle = title ?? "";
            _currentItemType = itemType ?? "Music";
            _trackStartTime = DateTime.Now;
            _trackDuration = duration;

            // Reset animation state
            _animProgress = 0f;
            _animOutProgress = 0f;
            _isAnimatingIn = false;
            _isAnimatingOut = false;
            _isShowingLowerThird = false;
            _hasShownInitialTitle = false;
            _animStartTime = DateTime.MinValue;
            _animOutStartTime = DateTime.MinValue;

            Console.WriteLine($"[CGRenderer] Track changed: {artist} - {title} ({itemType})");
        }

        // ═══════════════════════════════════════════════════════════
        // RENDER OVERLAY ON VIDEO BUFFER
        // ═══════════════════════════════════════════════════════════
        public static void RenderOverlay(byte[] videoBuffer, int width, int height)
        {
            if (!_settingsLoaded) LoadSettings();

            try
            {
                GCHandle handle = GCHandle.Alloc(videoBuffer, GCHandleType.Pinned);
                try
                {
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    using (Bitmap bmp = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, ptr))
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                        RenderAllElements(g, width, height);
                    }
                }
                finally
                {
                    handle.Free();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CGRenderer] Render error: {ex.Message}");
            }
        }

        private static void RenderAllElements(Graphics g, int w, int h)
        {
            // Update animation states
            UpdateAnimationState();

            // Render elements in order (back to front)
            if (_logoEnabled) RenderLogo(g, w, h);
            RenderAdditionalLogos(g, w, h);
            if (_clockEnabled) RenderClock(g, w, h);

            // Render based on item type
            if (_currentItemType == "Spot" || _currentItemType == "ADV")
            {
                if (_spotLabelEnabled) RenderSpotLabel(g, w, h);
            }
            else if (_currentItemType == "Music")
            {
                // Mostra persistent info solo se non c'è lower third visibile o in animazione
                bool lowerThirdVisible = _isShowingLowerThird || _isAnimatingIn || _isAnimatingOut;

                if (_persistentInfoEnabled && _hasShownInitialTitle && !lowerThirdVisible)
                {
                    RenderPersistentInfo(g, w, h);
                }

                // ✅ CORRETTO: Renderizza lower third anche durante le animazioni
                if (_lowerThirdEnabled && (_isShowingLowerThird || _isAnimatingIn || _isAnimatingOut))
                {
                    RenderLowerThird(g, w, h);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ANIMATION STATE MACHINE
        // ═══════════════════════════════════════════════════════════
        private static void UpdateAnimationState()
        {
            if (string.IsNullOrEmpty(_currentArtist) && string.IsNullOrEmpty(_currentTitle))
                return;

            if (_trackStartTime == DateTime.MinValue)
                return;

            double elapsedSec = (DateTime.Now - _trackStartTime).TotalSeconds;
            double remainingSec = _trackDuration.TotalSeconds - elapsedSec;

            // Check if we should show the initial lower third
            bool shouldShowStart = elapsedSec >= _lowerThirdDelayStart &&
                                   elapsedSec < (_lowerThirdDelayStart + _lowerThirdDuration + ANIM_DURATION_SEC * 2);

            // Check if we should show the end lower third
            bool shouldShowEnd = _lowerThirdShowAtEnd &&
                                 remainingSec <= _lowerThirdEndOffset &&
                                 remainingSec > (_lowerThirdEndOffset - _lowerThirdEndDuration - ANIM_DURATION_SEC * 2) &&
                                 remainingSec > 0;

            bool shouldShow = shouldShowStart || shouldShowEnd;

            // State transitions
            if (shouldShow && !_isShowingLowerThird && !_isAnimatingIn)
            {
                // Start animating in
                _isAnimatingIn = true;
                _isAnimatingOut = false;
                _animStartTime = DateTime.Now;
                _animProgress = 0f;
            }
            else if (!shouldShow && _isShowingLowerThird && !_isAnimatingOut)
            {
                // Start animating out
                _isAnimatingOut = true;
                _isAnimatingIn = false;
                _animOutStartTime = DateTime.Now;
                _animOutProgress = 0f;
            }

            // Update animation progress
            if (_isAnimatingIn)
            {
                float elapsed = (float)(DateTime.Now - _animStartTime).TotalSeconds;
                _animProgress = Math.Min(1f, elapsed / ANIM_DURATION_SEC);

                if (_animProgress >= 1f)
                {
                    _isAnimatingIn = false;
                    _isShowingLowerThird = true;
                }
            }
            else if (_isAnimatingOut)
            {
                float elapsed = (float)(DateTime.Now - _animOutStartTime).TotalSeconds;
                _animOutProgress = Math.Min(1f, elapsed / ANIM_DURATION_SEC);

                if (_animOutProgress >= 1f)
                {
                    _isAnimatingOut = false;
                    _isShowingLowerThird = false;
                    _hasShownInitialTitle = true;
                }
            }

            // Check if persistent info should be hidden
            if (_persistentInfoEnabled && _hasShownInitialTitle)
            {
                if (remainingSec <= _persistentInfoHideBeforeEnd || shouldShowEnd)
                {
                    // Hide persistent info
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // RENDER LOGO
        // ═══════════════════════════════════════════════════════════
        private static void RenderLogo(Graphics g, int w, int h)
        {
            if (_logoBitmap == null) return;

            int logoW = _logoBitmap.Width;
            int logoH = _logoBitmap.Height;
            int x = 0, y = 0;

            switch (_logoPosition)
            {
                case "TopLeft": x = _logoMargin; y = _logoMargin; break;
                case "TopRight": x = w - logoW - _logoMargin; y = _logoMargin; break;
                case "BottomLeft": x = _logoMargin; y = h - logoH - _logoMargin; break;
                case "BottomRight": x = w - logoW - _logoMargin; y = h - logoH - _logoMargin; break;
            }

            // Apply opacity
            ColorMatrix cm = new ColorMatrix();
            cm.Matrix33 = _logoOpacity / 100f;
            ImageAttributes ia = new ImageAttributes();
            ia.SetColorMatrix(cm);

            g.DrawImage(_logoBitmap, new Rectangle(x, y, logoW, logoH),
                0, 0, logoW, logoH, GraphicsUnit.Pixel, ia);
        }

        // ═══════════════════════════════════════════════════════════
        // RENDER CLOCK
        // ═══════════════════════════════════════════════════════════
        private static void RenderClock(Graphics g, int w, int h)
        {
            string time = DateTime.Now.ToString("HH:mm");

            using (Font f = new Font("Segoe UI", _clockFontSize, FontStyle.Bold))
            {
                SizeF size = g.MeasureString(time, f);
                int x = 0, y = 0;

                if (_clockUnderLogo && _logoEnabled && _logoBitmap != null)
                {
                    int logoW = _logoBitmap.Width;
                    int logoH = _logoBitmap.Height;

                    switch (_logoPosition)
                    {
                        case "TopRight":
                            x = w - _logoMargin - logoW + (logoW - (int)size.Width) / 2;
                            y = _logoMargin + logoH + 5;
                            break;
                        case "TopLeft":
                            x = _logoMargin + (logoW - (int)size.Width) / 2;
                            y = _logoMargin + logoH + 5;
                            break;
                        case "BottomRight":
                            x = w - _logoMargin - logoW + (logoW - (int)size.Width) / 2;
                            y = h - _logoMargin - logoH - (int)size.Height - 15;
                            break;
                        case "BottomLeft":
                            x = _logoMargin + (logoW - (int)size.Width) / 2;
                            y = h - _logoMargin - logoH - (int)size.Height - 15;
                            break;
                    }
                }
                else
                {
                    x = w - (int)size.Width - 20;
                    y = 20;
                }

                // Background
                if (_clockBgEnabled)
                {
                    using (SolidBrush bgBrush = new SolidBrush(_clockBgColor))
                    {
                        g.FillRectangle(bgBrush, x - 10, y - 5, size.Width + 20, size.Height + 10);
                    }
                }

                // Text
                using (SolidBrush textBrush = new SolidBrush(_clockColor))
                {
                    g.DrawString(time, f, textBrush, x, y);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // RENDER PERSISTENT INFO BAR
        // ═══════════════════════════════════════════════════════════
        private static void RenderPersistentInfo(Graphics g, int w, int h)
        {
            if (string.IsNullOrEmpty(_currentArtist) && string.IsNullOrEmpty(_currentTitle))
                return;

            // Check if should be hidden
            double elapsedSec = (DateTime.Now - _trackStartTime).TotalSeconds;
            double remainingSec = _trackDuration.TotalSeconds - elapsedSec;

            if (remainingSec <= _persistentInfoHideBeforeEnd)
                return;

            // Check for end title overlap
            if (_lowerThirdShowAtEnd && remainingSec <= _lowerThirdEndOffset)
                return;

            string text = $"{_currentArtist} - {_currentTitle}";

            using (Font f = new Font(_lowerThirdFontFamily, _persistentInfoFontSize, FontStyle.Bold))
            {
                SizeF size = g.MeasureString(text, f);
                int padding = 10;
                int barHeight = 4;
                int x = _persistentInfoMarginX;
                int y = _persistentInfoMarginY;
                int boxW = (int)size.Width + padding * 2;
                int boxH = (int)size.Height + padding + barHeight + 6;

                // Background
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                {
                    g.FillRectangle(bgBrush, x, y, boxW, boxH);
                }

                // Text
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                {
                    g.DrawString(text, f, textBrush, x + padding, y + padding / 2);
                }

                // Progress bar
                int barY = y + (int)size.Height + padding / 2 + 4;

                // Background bar
                using (SolidBrush barBgBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
                {
                    g.FillRectangle(barBgBrush, x + padding, barY, boxW - padding * 2, barHeight);
                }

                // Progress fill
                float progress = (float)(elapsedSec / _trackDuration.TotalSeconds);
                progress = Math.Max(0f, Math.Min(1f, progress));

                using (SolidBrush barFillBrush = new SolidBrush(_progressBarColor))
                {
                    int fillW = (int)((boxW - padding * 2) * progress);
                    g.FillRectangle(barFillBrush, x + padding, barY, fillW, barHeight);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // RENDER LOWER THIRD
        // ═══════════════════════════════════════════════════════════
        private static void RenderLowerThird(Graphics g, int w, int h)
        {
            if (string.IsNullOrEmpty(_currentArtist) && string.IsNullOrEmpty(_currentTitle))
                return;

            string titleText = _currentTitle;
            string artistText = _currentArtist;

            // Determine animation value
            float animValue;
            if (_isAnimatingIn) animValue = _animProgress;
            else if (_isAnimatingOut) animValue = 1f - _animOutProgress;
            else animValue = 1f;

            if (_lowerThirdEntranceTheme == 2)
            {
                RenderLowerThirdTheme2(g, w, h, titleText, artistText, animValue);
                return;
            }
            else if (_lowerThirdEntranceTheme == 3)
            {
                RenderLowerThirdTheme3(g, w, h, titleText, artistText, animValue);
                return;
            }

            using (Font titleFont = new Font(_lowerThirdFontFamily, _lowerThirdTitleFontSize, FontStyle.Bold))
            using (Font artistFont = new Font(_lowerThirdFontFamily, _lowerThirdArtistFontSize, FontStyle.Regular))
            {
                SizeF titleSize = g.MeasureString(titleText, titleFont);
                SizeF artistSize = g.MeasureString(artistText, artistFont);

                int padding = 20;
                int accentWidth = 6;
                int boxW, boxH;
                float textY1, textY2;

                if (_lowerThirdLayout == "SingleLine")
                {
                    string fullText = $"{artistText} - {titleText}";
                    SizeF fullSize = g.MeasureString(fullText, titleFont);
                    boxW = (int)fullSize.Width + padding * 2 + accentWidth;
                    boxH = (int)fullSize.Height + padding;
                    textY1 = padding / 2;
                    textY2 = 0;
                }
                else
                {
                    boxW = (int)Math.Max(titleSize.Width, artistSize.Width) + padding * 2 + accentWidth;
                    boxH = (int)(titleSize.Height + artistSize.Height) + padding;
                    textY1 = padding / 2;
                    textY2 = padding / 2 + titleSize.Height;
                }

                int baseX = 0, baseY = h - boxH - _lowerThirdMarginY;

                switch (_lowerThirdPosition)
                {
                    case "BottomLeft": baseX = _lowerThirdMarginX; break;
                    case "BottomCenter": baseX = (w - boxW) / 2; break;
                    case "BottomRight": baseX = w - boxW - _lowerThirdMarginX; break;
                }

                // ✅ CORRETTO: Calcola progress e posizione in base allo stato
                float drawX = baseX;
                float drawY = baseY;
                float alpha = 1f;

                // Applica l'animazione
                switch (_lowerThirdAnimation)
                {
                    case "SlideLeft":
                        // Entra da sinistra (fuori schermo) verso la posizione finale
                        drawX = baseX - (boxW + baseX) * (1f - animValue);
                        break;

                    case "SlideRight":
                        // Entra da destra (fuori schermo) verso la posizione finale
                        drawX = baseX + (w - baseX) * (1f - animValue);
                        break;

                    case "SlideUp":
                        // Entra dal basso (fuori schermo) verso la posizione finale
                        drawY = baseY + (h - baseY) * (1f - animValue);
                        break;

                    case "FadeIn":
                        alpha = animValue;
                        break;

                    case "ZoomIn":
                        float zoomScale = 0.3f + animValue * 0.7f;
                        int originalBoxW = boxW;
                        int originalBoxH = boxH;
                        boxW = (int)(originalBoxW * zoomScale);
                        boxH = (int)(originalBoxH * zoomScale);
                        drawX = baseX + originalBoxW * (1f - zoomScale) / 2;
                        drawY = baseY + originalBoxH * (1f - zoomScale) / 2;
                        alpha = animValue;
                        break;
                }

                // Background
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(
                    (int)(alpha * _lowerThirdBgColor.A),
                    _lowerThirdBgColor.R, _lowerThirdBgColor.G, _lowerThirdBgColor.B)))
                {
                    g.FillRectangle(bgBrush, drawX, drawY, boxW, boxH);
                }

                // Accent bar
                using (SolidBrush accentBrush = new SolidBrush(Color.FromArgb((int)(alpha * 255), _lowerThirdAccentColor)))
                {
                    g.FillRectangle(accentBrush, drawX, drawY, accentWidth, boxH);
                }

                // Text
                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb((int)(alpha * 255), _lowerThirdTextColor)))
                {
                    if (_lowerThirdLayout == "SingleLine")
                    {
                        string fullText = $"{artistText} - {titleText}";
                        g.DrawString(fullText, titleFont, textBrush, drawX + padding + accentWidth, drawY + textY1);
                    }
                    else if (_lowerThirdLayout == "TitleAbove")
                    {
                        g.DrawString(titleText, titleFont, textBrush, drawX + padding + accentWidth, drawY + textY1);
                        g.DrawString(artistText, artistFont, textBrush, drawX + padding + accentWidth, drawY + textY2);
                    }
                    else // ArtistAbove
                    {
                        g.DrawString(artistText, artistFont, textBrush, drawX + padding + accentWidth, drawY + textY1);
                        g.DrawString(titleText, titleFont, textBrush, drawX + padding + accentWidth, drawY + textY2);
                    }
                }
            }
        }

        /// <summary>
        /// Theme 2 - Staggered: Background slides in first, then title fades in, then artist fades in.
        /// </summary>
        private static void RenderLowerThirdTheme2(Graphics g, int w, int h, string titleText, string artistText, float animValue)
        {
            using (Font titleFont = new Font(_lowerThirdFontFamily, _lowerThirdTitleFontSize, FontStyle.Bold))
            using (Font artistFont = new Font(_lowerThirdFontFamily, _lowerThirdArtistFontSize, FontStyle.Regular))
            {
                SizeF titleSize = g.MeasureString(titleText, titleFont);
                SizeF artistSize = g.MeasureString(artistText, artistFont);

                int padding = 20;
                int accentWidth = 6;
                int boxW = (int)Math.Max(titleSize.Width, artistSize.Width) + padding * 2 + accentWidth;
                int boxH = (int)(titleSize.Height + artistSize.Height) + padding;

                int baseX = 0, baseY = h - boxH - _lowerThirdMarginY;
                switch (_lowerThirdPosition)
                {
                    case "BottomLeft": baseX = _lowerThirdMarginX; break;
                    case "BottomCenter": baseX = (w - boxW) / 2; break;
                    case "BottomRight": baseX = w - boxW - _lowerThirdMarginX; break;
                }

                float bgProgress = Math.Min(1f, animValue / 0.4f);
                float titleAlpha = animValue < 0.4f ? 0f : Math.Min(1f, (animValue - 0.4f) / 0.3f);
                float artistAlpha = animValue < 0.7f ? 0f : Math.Min(1f, (animValue - 0.7f) / 0.3f);

                float drawX = baseX;
                float drawY = baseY + (h - baseY) * (1f - bgProgress);

                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(
                    _lowerThirdBgColor.A, _lowerThirdBgColor.R, _lowerThirdBgColor.G, _lowerThirdBgColor.B)))
                    g.FillRectangle(bgBrush, drawX, drawY, boxW, boxH);

                using (SolidBrush accentBrush = new SolidBrush(_lowerThirdAccentColor))
                    g.FillRectangle(accentBrush, drawX, drawY, accentWidth, boxH);

                if (titleAlpha > 0)
                {
                    using (SolidBrush tb = new SolidBrush(Color.FromArgb((int)(titleAlpha * 255), _lowerThirdTextColor)))
                        g.DrawString(titleText, titleFont, tb, drawX + padding + accentWidth, drawY + padding / 2);
                }

                if (artistAlpha > 0)
                {
                    using (SolidBrush ab = new SolidBrush(Color.FromArgb((int)(artistAlpha * 255), _lowerThirdTextColor)))
                        g.DrawString(artistText, artistFont, ab, drawX + padding + accentWidth, drawY + padding / 2 + titleSize.Height);
                }
            }
        }

        /// <summary>
        /// Theme 3 - Cinematic: Two separate boxes - title slides from left, artist slides from right.
        /// </summary>
        private static void RenderLowerThirdTheme3(Graphics g, int w, int h, string titleText, string artistText, float animValue)
        {
            using (Font titleFont = new Font(_lowerThirdFontFamily, _lowerThirdTitleFontSize, FontStyle.Bold))
            using (Font artistFont = new Font(_lowerThirdFontFamily, _lowerThirdArtistFontSize - 2, FontStyle.Italic))
            {
                SizeF titleSize = g.MeasureString(titleText, titleFont);
                SizeF artistSize = g.MeasureString(artistText, artistFont);

                int padding = 16;
                int accentHeight = 4;
                int gap = 4;

                int titleBoxW = (int)titleSize.Width + padding * 2;
                int titleBoxH = (int)titleSize.Height + padding;
                int artistBoxW = (int)artistSize.Width + padding * 2;
                int artistBoxH = (int)artistSize.Height + padding;

                int baseY = h - titleBoxH - artistBoxH - gap - _lowerThirdMarginY;
                int baseX = _lowerThirdMarginX;
                switch (_lowerThirdPosition)
                {
                    case "BottomCenter": baseX = (w - Math.Max(titleBoxW, artistBoxW)) / 2; break;
                    case "BottomRight": baseX = w - Math.Max(titleBoxW, artistBoxW) - _lowerThirdMarginX; break;
                }

                float titleProgress = Math.Min(1f, animValue / 0.5f);
                float artistProgress = animValue < 0.3f ? 0f : Math.Min(1f, (animValue - 0.3f) / 0.5f);
                float accentAlpha = animValue < 0.6f ? 0f : Math.Min(1f, (animValue - 0.6f) / 0.4f);

                titleProgress = 1f - (1f - titleProgress) * (1f - titleProgress);
                artistProgress = 1f - (1f - artistProgress) * (1f - artistProgress);

                float titleDrawX = baseX - (baseX + titleBoxW) * (1f - titleProgress);
                float titleDrawY = baseY;
                float artistDrawX = baseX + (w - baseX) * (1f - artistProgress);
                float artistDrawY = baseY + titleBoxH + gap;

                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(220, _lowerThirdBgColor.R, _lowerThirdBgColor.G, _lowerThirdBgColor.B)))
                    g.FillRectangle(bgBrush, titleDrawX, titleDrawY, titleBoxW, titleBoxH);

                using (SolidBrush tb = new SolidBrush(Color.FromArgb((int)(titleProgress * 255), _lowerThirdTextColor)))
                    g.DrawString(titleText, titleFont, tb, titleDrawX + padding, titleDrawY + padding / 2);

                if (artistProgress > 0)
                {
                    int bgR = Math.Min(255, _lowerThirdBgColor.R + 20);
                    int bgG = Math.Min(255, _lowerThirdBgColor.G + 20);
                    int bgB = Math.Min(255, _lowerThirdBgColor.B + 20);
                    using (SolidBrush bgBrush2 = new SolidBrush(Color.FromArgb(160, bgR, bgG, bgB)))
                        g.FillRectangle(bgBrush2, artistDrawX, artistDrawY, artistBoxW, artistBoxH);

                    using (SolidBrush ab = new SolidBrush(Color.FromArgb((int)(artistProgress * 255), _lowerThirdTextColor)))
                        g.DrawString(artistText, artistFont, ab, artistDrawX + padding, artistDrawY + padding / 2);
                }

                if (accentAlpha > 0)
                {
                    float accentX = Math.Min(titleDrawX, artistDrawX);
                    float accentW = Math.Max(titleDrawX + titleBoxW, artistDrawX + artistBoxW) - accentX;
                    using (SolidBrush accentBrush = new SolidBrush(Color.FromArgb((int)(accentAlpha * 255), _lowerThirdAccentColor)))
                        g.FillRectangle(accentBrush, accentX, artistDrawY - gap, accentW, accentHeight);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // RENDER SPOT LABEL
        // ═══════════════════════════════════════════════════════════
        private static void RenderAdditionalLogos(Graphics g, int w, int h)
        {
            if (_additionalLogos == null || _additionalLogos.Count == 0)
                return;

            foreach (var logo in _additionalLogos)
            {
                if (logo == null || string.IsNullOrWhiteSpace(logo.ImagePath) || !File.Exists(logo.ImagePath))
                    continue;
                var logoPath = logo.ImagePath.Trim();
                if (!_visibleAdditionalLogos.Contains(logoPath))
                    continue;

                try
                {
                    using (var image = Image.FromFile(logo.ImagePath))
                    {
                        float logoScale = logo.Scale > 0f ? logo.Scale : 1.0f;
                        int drawW = Math.Max(1, (int)(image.Width * logoScale));
                        int drawH = Math.Max(1, (int)(image.Height * logoScale));
                        int x = 0;
                        int y = 0;
                        int marginX = Math.Max(0, logo.MarginX);
                        int marginY = Math.Max(0, logo.MarginY);

                        switch (logo.Position)
                        {
                            case "TopLeft":
                                x = marginX; y = marginY; break;
                            case "TopCenter":
                                x = (w - drawW) / 2; y = marginY; break;
                            case "TopRight":
                                x = w - drawW - marginX; y = marginY; break;
                            case "MiddleLeft":
                                x = marginX; y = (h - drawH) / 2; break;
                            case "MiddleCenter":
                                x = (w - drawW) / 2; y = (h - drawH) / 2; break;
                            case "MiddleRight":
                                x = w - drawW - marginX; y = (h - drawH) / 2; break;
                            case "BottomLeft":
                                x = marginX; y = h - drawH - marginY; break;
                            case "BottomCenter":
                                x = (w - drawW) / 2; y = h - drawH - marginY; break;
                            case "BottomRight":
                                x = w - drawW - marginX; y = h - drawH - marginY; break;
                            default:
                                x = marginX; y = h - drawH - marginY; break;
                        }

                        g.DrawImage(image, x, y, drawW, drawH);
                    }
                }
                catch { }
            }
        }

        private static void RenderSpotLabel(Graphics g, int w, int h)
        {
            using (Font f = new Font("Segoe UI", _spotLabelFontSize, FontStyle.Bold))
            {
                SizeF size = g.MeasureString(_spotLabelText, f);
                int padding = 12;
                int boxW = (int)size.Width + padding * 2;
                int boxH = (int)size.Height + padding;

                int x = _spotLabelMarginX;
                int y = _spotLabelMarginY;

                switch (_spotLabelPosition)
                {
                    case "TopLeft":
                        x = _spotLabelMarginX;
                        y = _spotLabelMarginY;
                        break;
                    case "TopCenter":
                        x = (w - boxW) / 2;
                        y = _spotLabelMarginY;
                        break;
                    case "TopRight":
                        x = w - boxW - _spotLabelMarginX;
                        y = _spotLabelMarginY;
                        break;
                    case "Bottom":
                    case "BottomCenter":
                        x = (w - boxW) / 2;
                        y = h - boxH - _spotLabelMarginY;
                        break;
                    case "BottomLeft":
                        x = _spotLabelMarginX;
                        y = h - boxH - _spotLabelMarginY;
                        break;
                    case "BottomRight":
                        x = w - boxW - _spotLabelMarginX;
                        y = h - boxH - _spotLabelMarginY;
                        break;
                }

                // Background
                if (_spotLabelBgEnabled)
                {
                    using (SolidBrush bgBrush = new SolidBrush(_spotLabelBgColor))
                    {
                        g.FillRectangle(bgBrush, x, y, boxW, boxH);
                    }
                }

                // Text
                using (SolidBrush textBrush = new SolidBrush(_spotLabelTextColor))
                {
                    g.DrawString(_spotLabelText, f, textBrush, x + padding, y + padding / 2);
                }
            }
        }
    }
}
