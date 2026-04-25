// WheelPickerView.cs
// 完整可复用 .NET MAUI 横向滚轮控件（支持：循环 / 吸附 / 渐隐 / 中心高亮）

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace Memorize_words.Controls
{
    public class WheelPickerView : ContentView
    {
        // ===== 可调参数 =====
        public double ItemWidth { get; set; } = 60;
        public int VisibleCount { get; set; } = 5;
        public double SnapDuration { get; set; } = 150;

        // ===== 数据 =====
        private List<int> _data;
        private List<Label> _labels = new();

        // ===== 状态 =====
        private double _offset;
        private double _lastPanX;
        private int _centerIndex;


        private AbsoluteLayout _root;

        public int SelectedValue => _data[_centerIndex % _data.Count];
        public event Action<int>? OnValueClicked;
        public event Action<int>? OnValueConfirmed;

        public WheelPickerView()
        {
            // ⭐ 核心黑科技：透明度为 1/255 的背景色。
            // 肉眼绝对看不见，但能强制 Android 和 iOS 生成触控拦截面，防止手势穿透掉。
            this.BackgroundColor = Color.FromRgba(255, 255, 255, 0.01);
            InitData();
            BuildUI();
            InitGesture();
            ResetToCenter();
        }

        private void InitData()
        {
            var baseData = Enumerable.Range(0, 8).ToList();
            _data = Enumerable.Repeat(baseData, 100).SelectMany(x => x).ToList();
        }

        private void BuildUI()
        {
            _root = new AbsoluteLayout
            {
                HeightRequest = 80,
                // ⭐ 修复1：加上透明背景色，确保整个区域能接收手势 (Hit-Test)
                BackgroundColor = Colors.Transparent
            };

            for (int i = 0; i < _data.Count; i++)
            {
                var lbl = new Label
                {
                    Text = _data[i].ToString(),
                    FontSize = 28,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    WidthRequest = ItemWidth,
                    HeightRequest = 80,
                    // ⭐ 修复2：开启手势穿透，禁止 Label 吞噬滑动事件
                    InputTransparent = true
                };

                _labels.Add(lbl);
                _root.Children.Add(lbl);
            }

            // 中心框
            var border = new Border
            {
                Stroke = Colors.Blue,
                StrokeThickness = 2,
                WidthRequest = ItemWidth,
                HeightRequest = 70,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                // ⭐ 修复3：框也要穿透
                InputTransparent = true
            };

            AbsoluteLayout.SetLayoutFlags(border, AbsoluteLayoutFlags.PositionProportional);
            AbsoluteLayout.SetLayoutBounds(border, new Rect(0.5, 0.5, ItemWidth, 70));

            _root.Children.Add(border);

            Content = _root;
        }

        // 2. 修改手势绑定，把手势直接绑在 _root 上，而不是 ContentView 本身
        private void InitGesture()
        {
            var pan = new PanGestureRecognizer();
            pan.PanUpdated += OnPanUpdated;
            // ⭐ 修复：绑在内部容器上
            _root.GestureRecognizers.Add(pan);

            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) =>
            {
                var pos = e.GetPosition(_root); // ⭐ 修复：以 _root 为基准计算
                if (pos.HasValue)
                {
                    double centerStart = (Width / 2) - (ItemWidth / 2);
                    double centerEnd = (Width / 2) + (ItemWidth / 2);

                    if (pos.Value.X >= centerStart && pos.Value.X <= centerEnd)
                    {
                        OnValueClicked?.Invoke(SelectedValue);
                    }
                }
            };
            // ⭐ 修复：绑在内部容器上
            _root.GestureRecognizers.Add(tap);
        }

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _lastPanX = e.TotalX;
                    break;

                case GestureStatus.Running:
                    var dx = e.TotalX - _lastPanX;
                    _lastPanX = e.TotalX;

                    // 反方向（关键）
                    _offset -= dx;

                    UpdateLayout();
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled: // 处理手机上滑动过快被系统取消的情况
                    SnapToNearest();
                    break;
            }
        }

        private void UpdateLayout()
        {
            double center = Width / 2;
            double minDist = double.MaxValue;
            int closestIndex = 0;

            for (int i = 0; i < _labels.Count; i++)
            {
                double x = i * ItemWidth - _offset;

                AbsoluteLayout.SetLayoutBounds(_labels[i], new Rect(x, 0, ItemWidth, 80));

                // 渐隐
                double itemCenter = x + ItemWidth / 2;
                double dist = Math.Abs(itemCenter - center);

                double maxDist = ItemWidth * 2;
                double opacity = 1 - Math.Min(dist / maxDist, 1);
                _labels[i].Opacity = opacity;

                // 中心放大
                double scale = 1 + 0.3 * (1 - Math.Min(dist / ItemWidth, 1));
                _labels[i].Scale = scale;

                if (dist < minDist)
                {
                    minDist = dist;
                    closestIndex = i;
                }
            }
            _centerIndex = closestIndex;
        }

        private async void SnapToNearest()
        {
            double center = Width / 2;

            double rawIndex = (_offset + center - ItemWidth / 2) / ItemWidth;
            int index = (int)Math.Round(rawIndex);

            _centerIndex = index;

            double targetOffset = index * ItemWidth - center + ItemWidth / 2;

            double start = _offset;
            double delta = targetOffset - start;

            int frames = 10;

            for (int i = 1; i <= frames; i++)
            {
                _offset = start + delta * (i / (double)frames);
                UpdateLayout();
                await System.Threading.Tasks.Task.Delay((int)(SnapDuration / frames));
            }

            _offset = targetOffset;
            UpdateLayout();
        }

        //private void ResetToCenter()
        //{
        //    _centerIndex = _data.Count / 2;
        //    _offset = _centerIndex * ItemWidth;

        //    SizeChanged += (_, __) =>
        //    {
        //        double center = Width / 2;
        //        _offset = _centerIndex * ItemWidth - center + ItemWidth / 2;
        //        UpdateLayout();
        //    };
        //}
        private bool _initialized = false;

        //private void ResetToCenter()
        //{
        //    _centerIndex = _data.Count / 2;

        //    SizeChanged += async (_, __) =>
        //    {
        //        if (_initialized || Width <= 0)
        //            return;

        //        _initialized = true;

        //        await Task.Yield(); // 等待本轮布局完全结束

        //        double center = Width / 2;

        //        _offset = _centerIndex * ItemWidth - center + ItemWidth / 2;

        //        UpdateLayout();

        //        SnapToNearest();   // ⭐ 强制最终吸附校正
        //    };
        //}


        //public void ScrollToValue(int value)
        //{
        //    double center = Width / 2;

        //    int bestIndex = -1;
        //    double minDist = double.MaxValue;

        //    for (int i = 0; i < _data.Count; i++)
        //    {
        //        if (_data[i] != value) continue;

        //        double x = i * ItemWidth - _offset;
        //        double itemCenter = x + ItemWidth / 2;

        //        double dist = Math.Abs(itemCenter - center);

        //        if (dist < minDist)
        //        {
        //            minDist = dist;
        //            bestIndex = i;
        //        }
        //    }

        //    if (bestIndex == -1) return;

        //    _centerIndex = bestIndex;
        //    _offset = bestIndex * ItemWidth - center + ItemWidth / 2;

        //    UpdateLayout();
        //}
        private void ResetToCenter()
        {
            // 默认中心点落在数组的正中间，比如 800个元素的第 400 个（保证左右都有充足的数据可滚）
            _centerIndex = _data.Count / 2;

            SizeChanged += (s, e) =>
            {
                if (Width <= 0) return;

                // ⭐ 修复1补全：抛弃 `_initialized` 单次锁定逻辑。
                // 确保无论是初次加载、弹窗、还是设备翻转，只要尺寸就绪/变化，就立刻以当前的 _centerIndex 算好 Offset
                double center = Width / 2;
                _offset = _centerIndex * ItemWidth - center + ItemWidth / 2;
                UpdateLayout();
            };
        }

        public void ScrollToValue(int value)
        {
            int bestIndex = -1;
            int minIndexDiff = int.MaxValue;

            // ⭐ 修复2：解决滚动到某些值时“左边是空白”的问题
            // 以前依靠 _offset 去判断距离，如果 _offset 没初始化(为0)，就会抓取到最开头的 Index 0 (此时左边没有元素了)
            // 修改后：只以当前的 _centerIndex 为基准，往左右两侧就近查找，完美保底处于中间安全区间。
            for (int i = 0; i < _data.Count; i++)
            {
                if (_data[i] == value)
                {
                    int diff = Math.Abs(i - _centerIndex);
                    if (diff < minIndexDiff)
                    {
                        minIndexDiff = diff;
                        bestIndex = i;
                    }
                }
            }

            if (bestIndex == -1) return;

            _centerIndex = bestIndex;

            // 如果布局已就绪，立即更新视图
            if (Width > 0)
            {
                double center = Width / 2;
                _offset = _centerIndex * ItemWidth - center + ItemWidth / 2;
                UpdateLayout();
            }
        }


    }
}