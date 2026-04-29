// ThreeWeekCalendarView.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

namespace Memorize_words.Controls
{
    public class ThreeWeekCalendarView : ContentView
    {
        public Color FutureTextColor { get; set; } = Colors.Black;
        public Color PastTextColor { get; set; } = Colors.Gray;
        //public Color TodayBorderColor { get; set; } = Colors.Blue;
        public Color TodayBorderColor =>IsDarkMode ? Colors.LightSkyBlue : Colors.Blue;

        private Grid _grid;
        private Label[] _headerLabels = new Label[7]; // 缓存表头
        private CellUI[] _cells = new CellUI[21];     // 缓存 21 个日历格子（核心优化）

        private DateTime Today => DateTime.Today;
        private DateTime _selectedDate;
        public bool IsDeleteMode { get; set; }
        public event Action<DateTime>? DateSelected;
        public event Action? DataChanged;

        Dictionary<DateTime, List<CalendarItem>> _type1Data;
        Dictionary<DateTime, List<CalendarItem>> _type2Data;
        private readonly int[] _offsets = new[] { 0, 1, 3, 7 };
        public WeekStartMode WeekStart { get; set; } = WeekStartMode.Monday;

        public DateTime SelectedDate => _selectedDate;

        public enum WeekStartMode { Monday, Sunday }
        // ⭐ 2. 判断当前主题并输出对应的动态蓝色
        private bool IsDarkMode => Application.Current?.RequestedTheme == AppTheme.Dark;
        private Color DynamicBlue => IsDarkMode ? Colors.LightSkyBlue : Colors.Blue;

        // ================= 内部缓存结构 =================
        private class CellUI
        {
            public Frame RootFrame { get; set; }
            public Label DateLabel { get; set; }
            public HorizontalStackLayout Type1Stack { get; set; }
            public HorizontalStackLayout Type2Stack { get; set; }
        }

        public ThreeWeekCalendarView()
        {
            Application.Current.RequestedThemeChanged += (_, __) =>
            {
                Render();
            };
            LoadData();
            _selectedDate = Today;
            var mode = Preferences.Get("WeekStart", "Monday");
            WeekStart = mode == "Sunday" ? WeekStartMode.Sunday : WeekStartMode.Monday;

            BuildUI(); // 只执行一次，建好所有的壳子
            Render();  // 往壳子里填数据
        }

        private string[] GetWeekHeaders()
        {
            return WeekStart == WeekStartMode.Monday
                ? new[] { "一", "二", "三", "四", "五", "六", "日" }
                : new[] { "日", "一", "二", "三", "四", "五", "六" };
        }

        // ================= 核心优化：只建一次壳子 =================
        private void BuildUI()
        {
            _grid = new Grid
            {
                RowSpacing = 6,
                ColumnSpacing = 6
            };

            for (int i = 0; i < 7; i++)
                _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int i = 0; i < 3; i++)
                _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            // 1. 初始化表头，并缓存引用
            for (int i = 0; i < 7; i++)
            {
                var label = new Label { HorizontalOptions = LayoutOptions.Center };
                _grid.Add(label, i, 0);
                _headerLabels[i] = label;
            }

            // 2. 预先创建 21 个日历格子，以后永不销毁！
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    int index = row * 7 + col;
                    _cells[index] = CreateEmptyCellUI();
                    _grid.Add(_cells[index].RootFrame, col, row + 1);
                }
            }

            Content = new Frame
            {
                CornerRadius = 10,
                Padding = 10,
                HasShadow = true,
                Content = _grid
            };
        }

        private CellUI CreateEmptyCellUI()
        {
            var cell = new CellUI();

            cell.DateLabel = new Label
            {
                FontSize = 16,
                TextColor = Colors.Black,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center
            };

            cell.Type1Stack = new HorizontalStackLayout { Spacing = 2, HorizontalOptions = LayoutOptions.Center };
            cell.Type2Stack = new HorizontalStackLayout { Spacing = 2, HorizontalOptions = LayoutOptions.Center };

            var layout = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(14) },
                    new RowDefinition { Height = new GridLength(14) }
                }
            };

            layout.Add(cell.DateLabel, 0, 0);
            layout.Add(cell.Type1Stack, 0, 1);
            layout.Add(cell.Type2Stack, 0, 2);

            cell.RootFrame = new Frame
            {
                Padding = 4,
                Content = layout,
                CornerRadius = 6
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += OnDateTapped;
            cell.RootFrame.GestureRecognizers.Add(tap);

            return cell;
        }

        private DateTime GetWeekStart(DateTime date)
        {
            if (WeekStart == WeekStartMode.Monday)
            {
                int diff = ((int)date.DayOfWeek + 6) % 7;
                return date.AddDays(-diff);
            }
            else
            {
                int diff = (int)date.DayOfWeek;
                return date.AddDays(-diff);
            }
        }

        // ================= 核心优化：只更新数据，不再 Remove/New UI =================
        private void Render()
        {
            if (_grid == null) return;

            // 1. 更新表头文字
            var days = GetWeekHeaders();
            for (int i = 0; i < 7; i++)
            {
                _headerLabels[i].Text = days[i];
            }

            // 2. 更新 21 个格子的内容
            var start = GetWeekStart(Today).AddDays(-7);

            for (int i = 0; i < 21; i++)
            {
                var cell = _cells[i];
                var date = start.AddDays(i);

                bool isPast = date.Date < Today;
                bool isSelected = date.Date == _selectedDate.Date;

                // 取数据
                _type1Data.TryGetValue(date.Date, out var l1);
                _type2Data.TryGetValue(date.Date, out var l2);

                int total = (l1?.Count ?? 0) + (l2?.Count ?? 0);

                // 计算颜色
                Color baseColor = Colors.White;
                if (total == 2) baseColor = Color.FromRgb(204, 255, 153);
                else if ( total >2 && total <= 4) baseColor = Color.FromRgb(255, 255, 153);
                else if (total > 4) baseColor = Color.FromRgb(255, 153, 153);

                if (isPast) baseColor = BlendWithGray(baseColor);

                // 更新基础属性
                cell.RootFrame.BindingContext = date; // 更新绑定的日期
                cell.DateLabel.Text = date.Day.ToString();
                cell.RootFrame.BackgroundColor = baseColor;
                cell.RootFrame.BorderColor = isSelected ? TodayBorderColor : Colors.Transparent;

                // 更新小角标（对象池复用机制）
                UpdateStackLayout(cell.Type1Stack, l1, Colors.Red, "Ⅰ");
                UpdateStackLayout(cell.Type2Stack, l2, Colors.Blue, "Ⅱ");
            }
        }

        // ================= 极简对象池：复用小角标标签 =================
        private void UpdateStackLayout(HorizontalStackLayout stack, List<CalendarItem>? items, Color color, string state)
        {
            int count = items?.Count ?? 0;

            // 如果当前 Stack 里的标签不够，补齐（只会补一次，以后就长久存在了）
            while (stack.Children.Count < count)
            {
                var lbl = new Label { FontSize = 10, TextColor = color };
                // 去掉了手势绑定，它现在只是一个纯粹的显示文本
                stack.Children.Add(lbl);
            }

            // 遍历 Stack 里的所有标签，有数据就显示，没数据就隐藏
            for (int i = 0; i < stack.Children.Count; i++)
            {
                var lbl = (Label)stack.Children[i];
                if (i < count)
                {
                    var item = items[i];
                    lbl.Text = item.Value.ToString() + ToSuperscript(item.Index);
                    lbl.BindingContext = item; // 存入数据供点击时读取
                    lbl.ClassId = state;       // 存入状态 "Ⅰ" 或 "Ⅱ"
                    lbl.IsVisible = true;
                }
                else
                {
                    lbl.IsVisible = false;
                    lbl.BindingContext = null; // 释放引用防泄漏
                }
            }
        }

        // ================= 高性能上标转换 =================
        private string ToSuperscript(int n)
        {
            // 取消原来的字符串 Select 拼接，用 switch 避免字符串内存分配
            return n switch
            {
                0 => "⁰",
                1 => "¹",
                2 => "²",
                3 => "³",
                4 => "⁴",
                5 => "⁵",
                6 => "⁶",
                7 => "⁷",
                8 => "⁸",
                9 => "⁹",
                _ => n.ToString()
            };
        }

        private Color BlendWithGray(Color color)
        {
            double factor = 0.5;
            return new Color(
                (float)(color.Red * (1 - factor) + 0.5 * factor),
                (float)(color.Green * (1 - factor) + 0.5 * factor),
                (float)(color.Blue * (1 - factor) + 0.5 * factor)
            );
        }

        private void OnDateTapped(object? sender, TappedEventArgs e)
        {
            if (sender is Frame frame && frame.BindingContext is DateTime date)
            {
                _selectedDate = date;
                Render();
                DateSelected?.Invoke(date);
            }
        }

        // ===== 核心接口（下方代码基本维持原逻辑，但运行速度已质变） =====

        public void AddValueFromWheel(int value, string state)
        {
            var dict = state == "Ⅰ" ? _type1Data : _type2Data;
            DateTime baseDate = _selectedDate.Date;

            for (int i = 0; i < _offsets.Length; i++)
            {
                var date = baseDate.AddDays(_offsets[i]);
                if (!dict.ContainsKey(date)) dict[date] = new List<CalendarItem>();

                dict[date].Add(new CalendarItem { Value = value, Index = i, BaseDate = baseDate });
            }

            Render();
            SaveData();
            DataChanged?.Invoke();
        }


        public int GetNextValue(string state)
        {
            var dict = state == "Ⅰ" ? _type1Data : _type2Data;

            DateTime maxBaseDate = DateTime.MinValue;

            // 1. 遍历所有数据，找出全局最大的 BaseDate（即最近一次创建日程的基准日期）
            foreach (var kvp in dict)
            {
                foreach (var item in kvp.Value)
                {
                    if (item.BaseDate > maxBaseDate)
                    {
                        maxBaseDate = item.BaseDate;
                    }
                }
            }

            if (maxBaseDate != DateTime.MinValue)
            {
                // 2. 找到该最大 BaseDate 下，最后添加的那个记录
                if (dict.TryGetValue(maxBaseDate, out var list))
                {
                    // list 维持了插入顺序，所以最后一个符合 BaseDate 的项就是最新创建的
                    var lastItem = list.LastOrDefault(x => x.BaseDate == maxBaseDate);
                    if (lastItem != null)
                    {
                        return (lastItem.Value + 1) % 8; // 保证 7 之后是 0
                    }
                }

                // 兜底逻辑（通常不会走到这里）
                var allItems = dict.Values.SelectMany(x => x).Where(x => x.BaseDate == maxBaseDate).ToList();
                if (allItems.Any())
                {
                    return (allItems.Last().Value + 1) % 8;
                }
            }

            return 0; // 如果没有任何日程，默认从 0 开始
        }

        void SaveData()
        {
            string Encode(Dictionary<DateTime, List<CalendarItem>> dict)
            {
                var parts = new List<string>(dict.Count);
                foreach (var kv in dict)
                {
                    string date = kv.Key.ToString("yyyy-MM-dd");
                    string values = string.Join(",", kv.Value.Select(v => $"{v.Value}_{v.Index}_{v.BaseDate:yyyy-MM-dd}"));
                    parts.Add($"{date}:{values}");
                }
                return string.Join("|", parts);
            }
            Preferences.Set("type1", Encode(_type1Data));
            Preferences.Set("type2", Encode(_type2Data));
        }

        void LoadData()
        {
            Dictionary<DateTime, List<CalendarItem>> Decode(string s)
            {
                var dict = new Dictionary<DateTime, List<CalendarItem>>();
                if (string.IsNullOrEmpty(s)) return dict;

                var items = s.Split('|', StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in items)
                {
                    var parts = item.Split(':');
                    if (parts.Length != 2) continue;

                    DateTime date = DateTime.ParseExact(parts[0], "yyyy-MM-dd", null);
                    var list = new List<CalendarItem>();
                    var entries = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var e in entries)
                    {
                        var seg = e.Split('_');
                        if (seg.Length == 3 && int.TryParse(seg[0], out int val) && int.TryParse(seg[1], out int idx) && DateTime.TryParse(seg[2], out DateTime baseDate))
                        {
                            list.Add(new CalendarItem { Value = val, Index = idx, BaseDate = baseDate });
                        }
                        else if (int.TryParse(e, out int oldVal))
                        {
                            list.Add(new CalendarItem { Value = oldVal, Index = 0, BaseDate = date });
                        }
                    }
                    dict[date] = list;
                }
                return dict;
            }
            _type1Data = Decode(Preferences.Get("type1", ""));
            _type2Data = Decode(Preferences.Get("type2", ""));
        }

        public void DeleteGroup(CalendarItem target, string state)
        {
            var dict = state == "Ⅰ" ? _type1Data : _type2Data;

            // 1. 安全获取当前点击 item 的偏移天数
            int targetOffset = 0;
            if (target.Index >= 0 && target.Index < _offsets.Length)
            {
                targetOffset = _offsets[target.Index];
            }

            // 2. 找到被点击的 item 实际所显示的日期
            DateTime targetDate = target.BaseDate.AddDays(targetOffset);

            // 3. 确定它在同 BaseDate 且同 Value 的重复记录中排第几个（精准定位，避免多点重复创建时误删）
            int occurrenceIndex = 0;
            if (dict.TryGetValue(targetDate, out var list))
            {
                int matchCount = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == target) // 内存引用严格相同，定位到了点击的具体对象
                    {
                        occurrenceIndex = matchCount;
                        break;
                    }
                    if (list[i].BaseDate == target.BaseDate && list[i].Value == target.Value)
                    {
                        matchCount++;
                    }
                }
            }

            // 4. 遍历该日程关联的所有偏移量（0, 1, 3, 7天），精准删除对应顺序的那唯一一个记录
            foreach (var offset in _offsets)
            {
                DateTime date = target.BaseDate.AddDays(offset);
                if (dict.TryGetValue(date, out var dateList))
                {
                    int matchCount = 0;
                    for (int i = 0; i < dateList.Count; i++)
                    {
                        if (dateList[i].BaseDate == target.BaseDate && dateList[i].Value == target.Value)
                        {
                            if (matchCount == occurrenceIndex)
                            {
                                dateList.RemoveAt(i);
                                break;
                            }
                            matchCount++;
                        }
                    }
                }
            }

            Render();
            SaveData();
            DataChanged?.Invoke();
        }
        public List<CalendarItem> GetItems(DateTime date, string state)
        {
            var dict = state == "Ⅰ" ? _type1Data : _type2Data;
            return dict.TryGetValue(date.Date, out var list) ? list : new List<CalendarItem>();
        }

        public void ApplySettingsFromPreferences()
        {
            var mode = Preferences.Get("WeekStart", "Monday");
            WeekStart = mode == "Sunday" ? WeekStartMode.Sunday : WeekStartMode.Monday;
            Render();
        }

        public void ReloadWeekStart() => Render();
    }
}