using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Memorize_words.Controls;

namespace Memorize_words.Controls
{

    public class DayDetailView : ContentView
    {
        private HorizontalStackLayout _type1Layout;
        private HorizontalStackLayout _type2Layout;

        public bool IsDeleteMode { get; set; }

        private ThreeWeekCalendarView? _calendar;
        private DateTime _currentDate;
        const int rowHeight = 34;
        public DayDetailView()
        {
            _type1Layout = new HorizontalStackLayout { Spacing = 6 };
            _type2Layout = new HorizontalStackLayout { Spacing = 6 };

            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
        {
            //// 第一行：Ⅰ + 布局 同排
            //new HorizontalStackLayout
            //{
            //    Spacing = 8,HeightRequest = rowHeight,
            //    Children =
            //    {
            //        new Label { Text = "□：", TextColor = Colors.Red, FontSize = 28 , FontAttributes=FontAttributes.Bold},
            //        _type1Layout
            //    }
            //},
            
            //// 第二行：Ⅱ + 布局 同排
            //new HorizontalStackLayout
            //{
            //    Spacing = 8,HeightRequest = rowHeight,
            //    Children =
            //    {
            //        new Label { Text = "⧅：", TextColor = Colors.Blue, FontSize = 25, FontAttributes=FontAttributes.None},
            //        _type2Layout
            //    }
            //}
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
                        FontSize = 30,           // ← 这里控制 □ 的大小（可调大到 34~38）
                        FontAttributes = FontAttributes.Bold
                    },
                    new Span
                    {
                        Text = "：",
                        TextColor = Colors.Red,
                        FontSize = 30,           // “：”保持原来大小或稍小
                        FontAttributes = FontAttributes.None
                    }
                }
            },
            VerticalOptions = LayoutOptions.Center,  // 垂直居中对齐
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
                Spans =
                {
                    new Span
                    {
                        Text = " ⧅ ",
                        TextColor = Colors.Blue,
                        FontSize = 30,           // ⧅ 通常本身较大，可比 □ 小一点
                        FontAttributes = FontAttributes.None
                    },
                    new Span
                    {
                        Text = "：",
                        TextColor = Colors.Blue,
                        FontSize = 30,
                        FontAttributes = FontAttributes.None
                    }
                }
            },
            VerticalOptions = LayoutOptions.Center
        },
        _type2Layout
    }
}
        }
            };
        
    }

        // 绑定日历
        public void BindCalendar(ThreeWeekCalendarView calendar)
        {
            _calendar = calendar;

            // ⭐ 1. 先同步当前日期（解决启动不显示）
            _currentDate = calendar.SelectedDate;
            Refresh();

            // ⭐ 2. 监听日期变化
            calendar.DateSelected += date =>
            {
                _currentDate = date;
                Refresh();
            };

            // ⭐ 3. 监听数据变化
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

            foreach (var item in list2)
                _type2Layout.Children.Add(CreateCell(item, Colors.Blue, "Ⅱ"));
        }

        private View CreateCell(CalendarItem item, Color color, string state)
        {
            var frame = new Frame
            {
                Padding = new Thickness(6, 2),
                CornerRadius = 6,
                WidthRequest = 35,
                BackgroundColor = Color.FromRgb(255, 255, 255),
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
