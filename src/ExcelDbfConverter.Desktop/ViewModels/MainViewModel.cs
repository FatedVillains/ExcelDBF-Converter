using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExcelDbfConverter.Desktop.Abstractions;

namespace ExcelDbfConverter.Desktop.ViewModels;

/// <summary>左侧导航项。</summary>
public sealed partial class NavigationItem : ObservableObject
{
    public required string Title { get; init; }

    public required object ViewModel { get; init; }

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>主窗口 ViewModel：左侧导航 + 内容区切换。</summary>
public sealed partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;

    public MainViewModel(
        INavigationService navigation,
        HomeViewModel home,
        ExcelToDbfViewModel excelToDbf,
        DbfToExcelViewModel dbfToExcel,
        BatchConversionViewModel batch,
        TemplateManagementViewModel templates,
        HistoryViewModel history,
        SettingsViewModel settings)
    {
        _navigation = navigation;

        NavItems = new ObservableCollection<NavigationItem>
        {
            new() { Title = "首页", ViewModel = home },
            new() { Title = "Excel → DBF", ViewModel = excelToDbf },
            new() { Title = "DBF → Excel", ViewModel = dbfToExcel },
            new() { Title = "批量转换", ViewModel = batch },
            new() { Title = "模板管理", ViewModel = templates },
            new() { Title = "转换历史", ViewModel = history },
            new() { Title = "设置", ViewModel = settings },
        };

        _navigation.CurrentViewModelChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CurrentViewModel));
            foreach (var item in NavItems)
            {
                item.IsSelected = ReferenceEquals(item.ViewModel, _navigation.CurrentViewModel);
            }
        };

        SelectedItem = NavItems[0];
    }

    public ObservableCollection<NavigationItem> NavItems { get; }

    public object? CurrentViewModel => _navigation.CurrentViewModel;

    public NavigationItem? SelectedItem
    {
        get => NavItems.FirstOrDefault(i => i.IsSelected);
        set
        {
            if (value is null)
            {
                return;
            }
            foreach (var item in NavItems)
            {
                item.IsSelected = ReferenceEquals(item, value);
            }
            _navigation.CurrentViewModel = value.ViewModel;
            OnPropertyChanged();
        }
    }
}
