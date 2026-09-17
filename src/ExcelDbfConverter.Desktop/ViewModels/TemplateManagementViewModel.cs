using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;
using ExcelDbfConverter.Desktop.Abstractions;

namespace ExcelDbfConverter.Desktop.ViewModels;

/// <summary>模板管理页面 ViewModel。</summary>
public sealed partial class TemplateManagementViewModel : ViewModelBase
{
    private readonly ITemplateService _templateService;
    private readonly IDialogService _dialogService;

    public TemplateManagementViewModel(ITemplateService templateService, IDialogService dialogService)
    {
        _templateService = templateService;
        _dialogService = dialogService;

        Templates = new ObservableCollection<TemplateInfo>();

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync);
        NewCommand = new RelayCommand(NewTemplate);
        ImportCommand = new AsyncRelayCommand(ImportAsync);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
    }

    [ObservableProperty]
    private TemplateInfo? _selectedTemplate;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editorName = string.Empty;

    [ObservableProperty]
    private string _editorDescription = string.Empty;

    public ObservableCollection<TemplateInfo> Templates { get; }

    public IAsyncRelayCommand RefreshCommand { get; }

    public IRelayCommand NewCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand DeleteCommand { get; }

    public IAsyncRelayCommand ImportCommand { get; }

    public IAsyncRelayCommand ExportCommand { get; }

    public async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        var templates = await _templateService.GetAllAsync();
        Templates.Clear();
        foreach (var t in templates)
        {
            Templates.Add(t);
        }
    }

    private void NewTemplate()
    {
        SelectedTemplate = null;
        EditorName = string.Empty;
        EditorDescription = string.Empty;
        IsEditing = true;
    }

    partial void OnSelectedTemplateChanged(TemplateInfo? value)
    {
        if (value is not null)
        {
            EditorName = value.Name;
            EditorDescription = value.Description;
            IsEditing = true;
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditorName))
        {
            _dialogService.ShowMessage("提示", "模板名称不能为空。");
            return;
        }

        var template = SelectedTemplate ?? new TemplateInfo();
        template.Name = EditorName.Trim();
        template.Description = EditorDescription?.Trim() ?? string.Empty;
        template.Version = string.IsNullOrEmpty(template.Version) ? "1.0" : template.Version;

        await _templateService.SaveAsync(template);
        await RefreshAsync();
        IsEditing = false;
    }

    private async Task DeleteAsync()
    {
        if (SelectedTemplate is null)
        {
            return;
        }
        if (!_dialogService.ShowConfirm("确认", $"确定删除模板「{SelectedTemplate.Name}」吗？"))
        {
            return;
        }
        await _templateService.DeleteAsync(SelectedTemplate.Id);
        await RefreshAsync();
        SelectedTemplate = null;
        IsEditing = false;
    }

    private async Task ImportAsync()
    {
        string? path = _dialogService.OpenFile("导入模板", "模板文件|*.exdbftemplate;*.json");
        if (path is null)
        {
            return;
        }
        try
        {
            await _templateService.ImportAsync(path);
            await RefreshAsync();
            _dialogService.ShowMessage("导入完成", "模板导入成功。");
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage("导入失败", ex.Message);
        }
    }

    private async Task ExportAsync()
    {
        if (SelectedTemplate is null)
        {
            return;
        }
        string? path = _dialogService.SaveFile("导出模板", SelectedTemplate.Name + ".exdbftemplate", "模板文件|*.exdbftemplate");
        if (path is null)
        {
            return;
        }
        try
        {
            await _templateService.ExportAsync(SelectedTemplate, path);
            _dialogService.ShowMessage("导出完成", "模板导出成功。");
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage("导出失败", ex.Message);
        }
    }
}
