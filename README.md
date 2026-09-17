# ExcelDBF Converter — Excel DBF 数据转换工具

一款运行于 Windows 的绿色免安装桌面工具，实现 Excel 文件与 Visual FoxPro DBF 文件之间的数据双向转换。完全本地离线处理，不上传任何文件数据。

## 功能

- **Excel → DBF**：Sheet 选择、数据预览、字段类型自动识别、字段手动配置（名称/类型/长度/小数位/顺序/是否导出）、自定义模板、数据校验、错误报告。
- **DBF → Excel**：DBF 结构读取、数据预览、字符编码自动识别（GBK/GB2312/UTF-8/ANSI）、Excel 格式与 Sheet 名称配置、大数据量自动拆分 Sheet。
- **批量转换**：多文件与文件夹批量转换、子目录扫描、重名处理、结果统计。
- **模板系统**：模板的创建、编辑、删除、导入、导出（`.exdbftemplate`，本质为 JSON）。
- **转换历史**：本地 SQLite 记录，支持重新打开文件目录、删除、清空。
- **系统设置**：默认输出目录、默认 Excel 格式、默认 DBF 编码、预览行数、自动文本识别关键词。

## 支持的文件格式

- Excel 输入/输出：`.xlsx`、`.xls`
- DBF：Visual FoxPro DBF（`.dbf`），字段类型 C / N / F / D / T / L / I / B
- 第一版不处理 `.fpt`（Memo）、`.cdx`、`.idx`、`.mdx`，不支持 CSV

## 技术栈

- .NET 8 + WPF + MVVM（CommunityToolkit.Mvvm）
- 依赖注入：Microsoft.Extensions.DependencyInjection
- Excel：NPOI
- DBF：自研 Visual FoxPro DBF Reader/Writer
- 本地存储：Microsoft.Data.Sqlite
- 日志：Serilog（滚动文件日志）

## 目录结构

```
ExcelDbfConverter
├── src
│   ├── ExcelDbfConverter.Core             # 模型、枚举、DTO、接口、常量
│   ├── ExcelDbfConverter.Application      # 服务、转换任务、校验、字段识别、模板/历史
│   ├── ExcelDbfConverter.Infrastructure   # NPOI Excel、DBF、SQLite、日志、配置
│   └── ExcelDbfConverter.Desktop          # WPF UI（MVVM）
├── tests
│   └── ExcelDbfConverter.Infrastructure.Tests
└── ExcelDbfConverter.sln
```

## 构建

```bash
dotnet build ExcelDbfConverter.sln -c Release
```

## 测试

```bash
dotnet test tests/ExcelDbfConverter.Infrastructure.Tests
```

## 发布（绿色免安装，win-x64，自包含）

```bash
dotnet publish src/ExcelDbfConverter.Desktop/ExcelDbfConverter.Desktop.csproj -c Release -r win-x64 --self-contained true -o publish
```

发布后目录（`publish/`）即为绿色版，直接双击 `ExcelDBFConverter.exe` 运行。运行时会在程序目录下自动创建 `data`（SQLite 数据库）、`templates`、`logs`、`config` 目录。

## DBF 兼容性说明

DBF 是本项目的技术核心，实现严格遵循 Visual FoxPro 表文件二进制规范：

- 表头：版本字节 `0x30`，记录数/表头长度/记录长度均小端；表头长度 = 296 + 32×字段数（含 `0x0D` 终止符与 263 字节 backlink）。
- 字段类型存储：`T`（DateTime）= 儒略日 + 毫秒（两个 32 位小端整数）；`I`（Integer）= 4 字节小端；`B`（Double）= 8 字节 IEEE-754 小端（**不做字节反转**）；`N`/`F` 为右对齐空格填充的 ASCII；`D` 为 8 字节 `YYYYMMDD`；`L` 为单字节。
- 代码页标记（偏移 0x1D）：GBK（936）写 `0x7A`，保证 Excel 与 VFP 正确读取中文。

详见 `VFP-DBF-Format-Reference.md`。
