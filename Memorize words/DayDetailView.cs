using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;

namespace Memorize_words.Controls
{
    public class DayDetailView : ContentView
    {
        private HorizontalStackLayout _type1Layout;
        private HorizontalStackLayout _type2Layout;

        // ⭐ 1. 保存对类型2(蓝色)文本元素的引用，以便主题切换时修改
        private Span _type2IconSpan;
        private Span _type2ColonSpan;

        public bool IsDeleteMode { get; set; }

        private ThreeWeekCalendarView? _calendar;
        private DateTime _currentDate;
        const int rowHeight = 34;

        // ⭐ 2. 判断当前主题并输出对应的动态蓝色
        private bool IsDarkMode => Application.Current?.RequestedTheme == AppTheme.Dark;
        private Color DynamicBlue => IsDarkMode ? Colors.LightSkyBlue : Colors.Blue;

        public DayDetailView()
        {
            _type1Layout = new HorizontalStackLayout { Spacing = 6 };
            _type2Layout = new HorizontalStackLayout { Spacing = 6 };

            // 提前实例化蓝色的 Span
            _type2IconSpan = new Span
            {
                Text = " ⧅ ",
                TextColor = DynamicBlue,
                FontSize = 30,
                FontAttributes = FontAttributes.None
            };

            _type2ColonSpan = new Span
            {
                Text = "：",
                TextColor = DynamicBlue,
                FontSize = 30,
                FontAttributes = FontAttributes.None
            };

            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    // 第一行：□ + 布局 同排 (永远保持红色)
                    new HorizontalStackLayout
                    {
                        Spacing = 4,
                        HeightRequest = rowHeight,
                        Children =
                        {
                            new Label
                            {
                                WidthRequest = 60,
                                FormattedText = new FormattedString
                                {
                                    Spans =
                                    {
                                        new Span
                                        {
                                            Text = "□",
                                            TextColor = Colors.Red,
                                            FontSize = 30,
                                            FontAttributes = FontAttributes.Bold
                                        },
                                        new Span
                                        {
                                            Text = "：",
                                            TextColor = Colors.Red,
                                            FontSize = 30,
                                            FontAttributes = FontAttributes.None
                                        }
                                    }
                                },
                                VerticalOptions = LayoutOptions.Center,
                                VerticalTextAlignment = TextAlignment.Center
                            },
                            _type1Layout
                        }
                    },

                    // 第二行：Ⅱ + 布局 同排
                    new HorizontalStackLayout
                    {
                        Spacing = 4,
                        HeightRequest = rowHeight,
                        Children =
                        {
                            new Label
                            {
                                WidthRequest = 60,
                                FormattedText = new FormattedString
                                {
                                    // 放入刚刚单独实例化的动态颜色 Span
                                    Spans = { _type2IconSpan, _type2ColonSpan }
                                },
                                VerticalOptions = LayoutOptions.Center
                            },
                            _type2Layout
                        }
                    }
                }
            };

            // ⭐ 3. 订阅系统主题切换事件
            if (Application.Current != null)
            {
                Application.Current.RequestedThemeChanged += OnThemeChanged;
            }
        }

        // ⭐ 4. 主题切换时触发UI变色逻辑
        private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _type2IconSpan.TextColor = DynamicBlue;
                _type2ColonSpan.TextColor = DynamicBlue;

                // 强制刷新内部的子块列表颜色
                Refresh();
            });
        }

        // 绑定日历
        public void BindCalendar(ThreeWeekCalendarView calendar)
        {
            _calendar = calendar;

            _currentDate = calendar.SelectedDate;
            Refresh();

            calendar.DateSelected += date =>
            {
                _currentDate = date;
                Refresh();
            };

            calendar.DataChanged += () =>
            {
                Refresh();
            };
        }

        // 刷新显示
        public void Refresh()
        {
            if (_calendar == null) return;

            _type1Layout.Children.Clear();
            _type2Layout.Children.Clear();

            var list1 = _calendar.GetItems(_currentDate, "Ⅰ");
            var list2 = _calendar.GetItems(_currentDate, "Ⅱ");

            foreach (var item in list1)
                _type1Layout.Children.Add(CreateCell(item, Colors.Red, "Ⅰ"));

            // ⭐ 5. 这里将写死的 Colors.Blue 改为 DynamicBlue
            foreach (var item in list2)
                _type2Layout.Children.Add(CreateCell(item, DynamicBlue, "Ⅱ"));
        }
        private Color DynamicBorderColor =>
        Application.Current?.RequestedTheme == AppTheme.Dark
            ? Colors.LightGray
            : Colors.Gray;
        private View CreateCell(CalendarItem item, Color color, string state)
        {
            var frame = new Frame
            {
                Padding = new Thickness(6, 2),
                CornerRadius = 6,
                WidthRequest = 35,
                BackgroundColor = Colors.Transparent,
                BorderColor = DynamicBorderColor,
                Content = new Label
                {
                    Text = item.Value.ToString() + ToSuperscript(item.Index),
                    FontSize = 20,
                    TextColor = color
                }
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) =>
            {
                if (IsDeleteMode && _calendar != null)
                {
                    _calendar.DeleteGroup(item, state);
                    Refresh(); // 删除后刷新
                }
            };

            frame.GestureRecognizers.Add(tap);

            return frame;
        }

        private string ToSuperscript(int n)
        {
            string[] sup = { "⁰", "¹", "²", "³", "⁴", "⁵", "⁶", "⁷", "⁸", "⁹" };
            return string.Concat(n.ToString().Select(c => sup[c - '0']));
        }
    }
}