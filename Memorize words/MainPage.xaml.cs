//using Android.Widget;

namespace Memorize_words; // 请将命名空间替换为你的项目实际命名空间
using Memorize_words.Controls;
using Microsoft.Maui.Controls;
using System.Globalization;


public partial class MainPage : ContentPage
{
    // 滚轮状态控制字段
    private bool _isScrolling;
    private double _lastScrollX;
    private int _selectedNumber;

    // ==========================================
    // ⚙️ 配置区：更改显示字符或内部标识只需修改此处
    // ==========================================

    // 【左侧按钮（WordType）配置】
    // UI显示的字符（在这里修改成你想要的任何字符，如 "A" 和 "B"，"正" 和 "反" 等）
    private const string WordType1_DisplayText = "□";
    private const string WordType2_DisplayText = "⧅";

    // 内部逻辑标识码（传给日历和保存到本地的值。默认保持"Ⅰ"和"Ⅱ"可防止老用户旧数据失效，如不考虑历史兼容，也可改为 "Type1", "Type2"）
    private const string WordType1_InternalKey = "Ⅰ";
    private const string WordType2_InternalKey = "Ⅱ";

    private enum WordTypeMode { Type1, Type2 }
    private WordTypeMode _currentWordMode = WordTypeMode.Type1;

    // 【右侧按钮（WriteDelete）配置】
    private const string WriteMode_DisplayText = "写";
    private const string DeleteMode_DisplayText = "删";

    private enum EditMode { Write, Delete }
    private EditMode _currentEditMode = EditMode.Write;

    // ==========================================
    private bool IsDarkMode => Application.Current?.RequestedTheme == AppTheme.Dark;

    public MainPage()
    {
        InitializeComponent();

        // 加载左按钮的持久化状态
        LoadWordTypeState();

        // 设置右按钮默认状态为“写”（白底黑字）
        SetEditModeState(EditMode.Write);

        wheelPicker.OnValueClicked += OnWheelClicked;
        detailView.BindCalendar(calendar);
        UpdateCountdown();
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);
        ToolbarItems.Clear();
        MessagingCenter.Subscribe<object>(this, "WeekStartChanged", (sender) =>
        {
            calendar.ReloadWeekStart();   // 你需要新增这个方法
        });
        MessagingCenter.Subscribe<App>(this, "DateChangedWhileBackgrounded", (sender) =>
        {
            RefreshForNewDay();
        });
        // ⭐ 核心2：订阅系统深色模式的实时切换事件
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeChanged += OnThemeChanged;
        }
    }

    // ⭐ 核心3：当用户在手机系统里切换深色模式时，立即刷新UI颜色
    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateWordTypeUI();
            SetEditModeState(_currentEditMode);
            UpdateCountdown();
            UpdateStatusBarAppearance();
        });
    }
    private void RefreshForNewDay()
    {
        calendar.ApplySettingsFromPreferences();            
        detailView.Refresh();
        UpdateCountdown();

        string key = GetCurrentWordTypeInternalKey();
        wheelPicker.ScrollToValue(calendar.GetNextValue(key));
    }
    private void UpdateCountdown()
    {
        var saved = Preferences.Get("TargetDate", "");

        if (!DateTime.TryParse(saved, out var target))
        {
            LblCountdown.Text = "";
            return;
        }

        int days = (target.Date - DateTime.Today).Days;

        if (days < 0)
        {
            LblCountdown.Text = "∅";
            LblCountdown.TextColor = Colors.Red; // 保持红色
        }
        else if (days == 0)
        {
            LblCountdown.Text = "0";
            LblCountdown.TextColor = Colors.Red; // 保持红色
        }
        else
        {
            LblCountdown.Text = $"{days}";
            // ⭐ 动态反色：深色模式白色，浅色模式黑色
            LblCountdown.TextColor = IsDarkMode ? Colors.White : Colors.Black;
        }
    }

    private async void OnTargetdateClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new dateSettingsPage());
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SettingsPage());
    }

    // ---------- 左按钮逻辑（彻底脱钩状态与UI） ----------

    private void LoadWordTypeState()
    {
        // 读取保存的内部标识（默认使用 Type1 的内部标识）
        string savedState = Preferences.Get("WordTypeState", WordType1_InternalKey);

        // 根据内部标识恢复实际的枚举状态
        _currentWordMode = (savedState == WordType2_InternalKey) ? WordTypeMode.Type2 : WordTypeMode.Type1;

        // 更新UI
        UpdateWordTypeUI();
    }

    // 原代码中有两个Click事件绑定，统一调用这个通用切换逻辑
    private void OnWordTypeToggleClicked(object sender, EventArgs e) => ToggleWordTypeMode();
    private void OnBtnWordTypeClicked(object sender, EventArgs e) => ToggleWordTypeMode();

    private void ToggleWordTypeMode()
    {
        // 1. 切换逻辑状态（枚举）
        _currentWordMode = _currentWordMode == WordTypeMode.Type1 ? WordTypeMode.Type2 : WordTypeMode.Type1;

        // 2. 拿到当前的内部逻辑标识（用于后端和本地存储）
        string internalKey = GetCurrentWordTypeInternalKey();

        // 3. 保存到 Preferences
        Preferences.Set("WordTypeState", internalKey);

        // 4. 更新UI显示字符和样式
        UpdateWordTypeUI();

        // 5. 联动滚轮：使用内部标识向日历查询下一个值
        int next = calendar.GetNextValue(internalKey);
        wheelPicker.ScrollToValue(next);
    }

    // 辅助方法：获取当前枚举对应的内部标识符
    private string GetCurrentWordTypeInternalKey()
    {
        return _currentWordMode == WordTypeMode.Type1 ? WordType1_InternalKey : WordType2_InternalKey;
    }

    private void UpdateWordTypeUI()
    {
        // ⭐ 背景统一设为透明，避免深色模式下出现白底
        BtnWordType.BackgroundColor = Colors.Transparent;

        if (_currentWordMode == WordTypeMode.Type1)
        {
            BtnWordType.Text = WordType1_DisplayText;
            BtnWordType.TextColor = Colors.Red; // 规定红色始终为红色
            BtnWordType.FontAttributes = FontAttributes.Bold;
            BtnWordType.FontSize = 28;
        }
        else
        {
            BtnWordType.Text = WordType2_DisplayText;
            // 纯蓝色在深色模式下极难看清，若深色模式自动换成浅蓝色(LightSkyBlue)，浅色保留纯蓝
            BtnWordType.TextColor = IsDarkMode ? Colors.LightSkyBlue : Colors.Blue;
            BtnWordType.FontSize = 28;
            BtnWordType.FontAttributes = FontAttributes.None;
        }
    }

    // ---------- 右按钮逻辑（解耦为枚举驱动） ----------

    private void BtnWriteDelete_Clicked(object sender, EventArgs e)
    {
        // 切换写入/删除逻辑状态
        _currentEditMode = _currentEditMode == EditMode.Write ? EditMode.Delete : EditMode.Write;

        SetEditModeState(_currentEditMode);
    }

    private void SetEditModeState(EditMode mode)
    {
        _currentEditMode = mode;
        bool isDelete = (_currentEditMode == EditMode.Delete);

        //// 更新UI字符和样式
        //BtnWordType.Text = isDelete ? DeleteMode_DisplayText : WriteMode_DisplayText;

        if (mode == EditMode.Write)
        {
            BtnWriteDelete.Text = WriteMode_DisplayText;
            // ⭐ 正常模式：背景透明，文字反转
            BtnWriteDelete.BackgroundColor = Colors.Transparent;
            BtnWriteDelete.TextColor = IsDarkMode ? Colors.White : Colors.Black;
        }
        else // Delete
        {
            BtnWriteDelete.Text = DeleteMode_DisplayText;
            // ⭐ 删除模式：被指定为红色，始终保持红底白字
            BtnWriteDelete.BackgroundColor = Colors.Red;
            BtnWriteDelete.TextColor = Colors.White;
        }

        // ⭐ 核心：状态同步给两个控件
        calendar.IsDeleteMode = isDelete;
        detailView.IsDeleteMode = isDelete;
    }

    // ---------- 滚轮逻辑 ----------
    private void OnWheelClicked(int value)
    {
        // 1️⃣ 判断是否“写”模式（对比枚举，不依赖UI字符）
        if (_currentEditMode != EditMode.Write)
            return;

        // 2️⃣ 获取当前类型的 内部逻辑标识（不要直接拿Btn的Text）
        string internalKey = GetCurrentWordTypeInternalKey();

        // 3️⃣ 写入日历
        calendar.AddValueFromWheel(value, internalKey);

        // 4️⃣ 计算下一个默认值
        int next = calendar.GetNextValue(internalKey);

        // 5️⃣ 滚轮跳转
        wheelPicker.ScrollToValue(next);
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();
        detailView?.Refresh();
        UpdateCountdown(); // ⭐ 每次返回页面都刷新

        // ⭐ 新增：每次从设置页返回主页时，强制日历读取新设置并重绘
        calendar.ApplySettingsFromPreferences();
        string internalKey = GetCurrentWordTypeInternalKey();
        int next = calendar.GetNextValue(internalKey);
        wheelPicker.ScrollToValue(next);
        // ⭐ 每次重新进入该页面时，也强制更新一次颜色，以防用户在后台设置里切换了主题
        UpdateWordTypeUI();
        SetEditModeState(_currentEditMode);
        UpdateCountdown();
        UpdateStatusBarAppearance();
    }
    // ==========================================
    // ⭐ 核心新增：专门处理手机顶部状态栏颜色的方法
    // ==========================================
    private void UpdateStatusBarAppearance()
    {
        bool isDark = IsDarkMode;

        // 1. 设置页面整体背景色（iOS 检测到背景色变化后，会自动将状态栏文字变为黑/白反色）
        this.BackgroundColor = isDark ? Colors.Black : Colors.White;

#if ANDROID
        // 2. 针对 Android 平台，调用底层 API 强制修改顶部状态栏的文字和背景颜色
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        var window = activity?.Window;
        if (window != null)
        {
            // 修改状态栏的背景色
            window.SetStatusBarColor(isDark ? Android.Graphics.Color.Black : Android.Graphics.Color.White);
            
            // 修改状态栏文字和图标的颜色
            var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView);
            if (controller != null)
            {
                // AppearanceLightStatusBars = true 意味着“亮色状态栏” -> 会强制将电量/时间的文字和图标变为黑色
                // 反之，false 时文字和图标为白色
                controller.AppearanceLightStatusBars = !isDark;
            }
        }
#endif
    }
    }