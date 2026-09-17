using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExcelDbfConverter.Core.Constants;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;
using ExcelDbfConverter.Desktop.Abstractions;

namespace ExcelDbfConverter.Desktop.ViewModels;

/// <summary>首页 ViewModel：功能入口 + 最近转换记录。</summary>
public sealed partial class HomeViewModel : ViewModelBase
{
    private readonly IHistoryService _history;
    private readonly INavigationService _navigation;
    private readonly ExcelToDbfViewModel _excelToDbf;
    private readonly DbfToExcelViewModel _dbfToExcel;

    public HomeViewModel(
        IHistoryService history,
        INavigationService navigation,
        ExcelToDbfViewModel excelToDbf,
        DbfToExcelViewModel dbfToExcel)
    {
        _history = history;
        _navigation = navigation;
        _excelToDbf = excelToDbf;
        _dbfToExcel = dbfToExcel;
        RecentHistory = new ObservableCollection<ConversionHistoryEntry>();

        GoExcelToDbfCommand = new RelayCommand(() => _navigation.CurrentViewModel = _excelToDbf);
        GoDbfToExcelCommand = new RelayCommand(() => _navigation.CurrentViewModel = _dbfToExcel);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public ObservableCollection<ConversionHistoryEntry> RecentHistory { get; }

    public IRelayCommand GoExcelToDbfCommand { get; }

    public IRelayCommand GoDbfToExcelCommand { get; }

    public IAsyncRelayCommand RefreshCommand { get; }

    public async Task RefreshAsync()
    {
        var items = await _history.GetRecentAsync(AppDefaults.RecentHistoryCount);
        RecentHistory.Clear();
        foreach (var item in items)
        {
            RecentHistory.Add(item);
        }
    }
}
