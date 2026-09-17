# ExcelDBF Converter

## Excel DBF 数据转换工具

---

# 1. 项目概述

## 1.1 项目名称

- 英文名称：ExcelDBF Converter
- 中文名称：Excel DBF 数据转换工具

---

## 1.2 项目定位

开发一款运行于 Windows 系统的绿色免安装桌面工具，实现 Excel 文件与 Visual FoxPro DBF 文件之间的数据双向转换。

软件完全采用本地离线处理模式，所有文件读取、数据转换、模板保存、历史记录均在用户本地完成，不依赖服务器，不上传任何文件数据。

主要功能包括：

- Excel 转 DBF
- DBF 转 Excel
- Excel Sheet 选择
- Excel 数据预览
- DBF 数据预览
- 字段自动识别
- 字段手动配置
- Visual FoxPro 字段类型支持
- 自定义字段映射
- 自定义转换模板
- 模板导入导出
- 批量文件转换
- 文件夹批量转换
- 转换进度显示
- 数据校验
- 错误报告
- 转换历史记录

---

# 2. 产品运行环境

## 2.1 操作系统

支持：

- Windows 10
- Windows 11

暂不考虑：

- macOS
- Linux
- Web 浏览器

---

## 2.2 软件运行方式

软件采用绿色免安装模式。

用户下载后解压即可运行。

例如：

```text
ExcelDBFConverter
│
├── ExcelDBFConverter.exe
├── config
├── templates
├── data
└── logs
```

用户直接双击：

```text
ExcelDBFConverter.exe
```

即可启动。

---

## 2.3 数据处理原则

必须满足以下要求：

1. 软件完全离线运行。
2. 不依赖任何远程服务器。
3. 不上传用户文件。
4. 不采集用户文件内容。
5. 所有转换操作在本地完成。
6. 模板保存在本地。
7. 历史记录保存在本地。
8. 错误日志保存在本地。

---

# 3. 支持的文件格式

## 3.1 Excel 输入格式

支持：

```text
.xlsx
.xls
```

暂不支持：

```text
.csv
.xlsm
.ods
```

---

## 3.2 Excel 输出格式

DBF 转 Excel时支持：

```text
.xlsx
.xls
```

默认推荐：

```text
.xlsx
```

---

## 3.3 DBF 格式

支持：

```text
Visual FoxPro DBF
```

只处理：

```text
.dbf
```

暂不处理：

```text
.fpt
.cdx
.idx
.mdx
```

因此第一版本不支持 Memo 文件关联读取。

如果 DBF 文件包含依赖 `.fpt` 的 Memo 数据，软件应提示用户该字段可能无法完整读取。

---

# 4. 系统整体功能结构

```text
ExcelDBF Converter
│
├── 首页
│
├── Excel 转 DBF
│   ├── 文件选择
│   ├── Sheet选择
│   ├── 数据预览
│   ├── 字段自动识别
│   ├── 字段配置
│   ├── 模板选择
│   ├── 数据校验
│   └── 文件转换
│
├── DBF 转 Excel
│   ├── 文件选择
│   ├── DBF结构读取
│   ├── 数据预览
│   ├── Excel配置
│   └── 文件转换
│
├── 批量转换
│   ├── 多文件转换
│   └── 文件夹转换
│
├── 模板管理
│   ├── 新建模板
│   ├── 编辑模板
│   ├── 复制模板
│   ├── 删除模板
│   ├── 导入模板
│   └── 导出模板
│
├── 转换历史
│
└── 系统设置
```

---

# 5. 首页设计

首页提供主要功能入口。

页面建议：

```text
┌───────────────────────────────────────────────┐
│ ExcelDBF Converter                            │
├───────────────────────────────────────────────┤
│                                               │
│                                               │
│       ┌──────────────┐  ┌──────────────┐     │
│       │              │  │              │     │
│       │ Excel 转 DBF │  │ DBF 转 Excel │     │
│       │              │  │              │     │
│       └──────────────┘  └──────────────┘     │
│                                               │
│                                               │
├───────────────────────────────────────────────┤
│ 最近转换记录                                  │
│                                               │
│ 文件名称          转换类型       时间         │
│ student.xlsx      Excel→DBF     10:20        │
│ student.dbf       DBF→Excel     09:30        │
│                                               │
└───────────────────────────────────────────────┘
```

功能入口：

- Excel 转 DBF
- DBF 转 Excel
- 批量转换
- 模板管理
- 转换历史
- 系统设置

---

# 6. Excel 转 DBF

## 6.1 操作流程

```text
选择Excel文件
      ↓
读取Excel文件
      ↓
选择Sheet
      ↓
读取表头
      ↓
选择转换模板（可选）
      ↓
自动识别字段类型
      ↓
用户调整字段配置
      ↓
数据预览
      ↓
数据校验
      ↓
选择输出位置
      ↓
开始转换
      ↓
生成DBF文件
```

---

# 7. Excel 文件选择

支持选择：

```text
.xlsx
.xls
```

界面：

```text
┌────────────────────────────────────┐
│ Excel 转 DBF                       │
├────────────────────────────────────┤
│                                    │
│ Excel文件：                         │
│                                    │
│ [选择文件]                         │
│                                    │
│ 或者                               │
│                                    │
│        拖拽Excel文件到这里          │
│                                    │
└────────────────────────────────────┘
```

功能要求：

- 支持文件选择。
- 支持拖拽文件。
- 校验文件格式。
- 文件不存在时提示。
- 文件被占用时提示。
- Excel 文件读取失败时提示具体原因。

---

# 8. Excel Sheet 选择

如果 Excel 文件存在多个 Sheet。

显示：

```text
Sheet：

[Sheet1 ▼]
```

支持：

- 显示所有 Sheet。
- 切换 Sheet。
- 显示每个 Sheet 数据行数。
- 显示每个 Sheet 列数。

例如：

| Sheet名称 | 行数 | 列数 |
|---|---:|---:|
| 学生信息 | 1000 | 10 |
| 成绩信息 | 5000 | 8 |

---

# 9. Excel 数据预览

读取 Excel 后显示数据预览。

例如：

| 姓名 | 身份证号 | 年龄 | 成绩 | 出生日期 |
|---|---|---:|---:|---|
| 张三 | 140101... | 20 | 90.5 | 2000-01-01 |
| 李四 | 140102... | 21 | 88.0 | 1999-02-02 |

默认：

```text
最多预览前100条数据
```

支持配置：

```text
100条
500条
1000条
```

数据预览要求：

- 支持横向滚动。
- 支持纵向滚动。
- 支持显示总数据行数。
- 支持显示总字段数。
- 不建议一次性加载全部数据到 UI。

---

# 10. Excel 字段自动识别

系统读取 Excel 数据后，自动分析字段类型。

自动分析建议读取：

```text
前100行有效数据
```

进行类型识别。

---

## 10.1 Character 类型识别

如果字段包含：

- 中文
- 英文
- 混合字符
- 长数字字符串
- 编号

识别为：

```text
Character
```

DBF 标识：

```text
C
```

---

## 10.2 Numeric 类型识别

如果数据全部为：

```text
1
2
3
100
```

识别：

```text
Numeric
```

DBF：

```text
N
```

自动计算：

- 字段长度
- 小数位数

例如：

```text
12345
```

建议：

```text
N(5,0)
```

---

## 10.3 小数识别

例如：

```text
90.5
88.25
100.00
```

识别：

```text
Numeric
```

例如：

```text
N(10,2)
```

自动计算最大整数长度和小数位数。

---

## 10.4 Date 类型识别

支持识别：

```text
2026-01-01

2026/01/01

2026年01月01日
```

识别：

```text
Date
```

DBF：

```text
D
```

---

## 10.5 DateTime 类型识别

例如：

```text
2026-01-01 10:30:00
```

识别：

```text
DateTime
```

DBF：

```text
T
```

---

## 10.6 Logical 类型识别

支持识别：

```text
true
false

TRUE
FALSE

是
否

Y
N

1
0
```

转换为：

```text
Logical
```

DBF：

```text
L
```

---

## 10.7 强制文本识别

以下类型字段即使全部由数字组成，也不应该自动识别为 Numeric。

例如：

```text
身份证号
手机号
学号
考生号
编号
代码
邮政编码
银行卡号
```

原因：

```text
可能存在前导0
数字长度超过普通数值范围
Excel可能存在科学计数法
```

因此提供规则：

```text
字段名称包含：

编号
代码
号码
身份证
手机
电话
学号
考生号

自动识别为 Character。
```

用户可以在设置中维护关键词。

---

# 11. DBF 字段配置

这是 Excel → DBF 的核心配置页面。

界面：

| 序号 | Excel字段 | DBF字段 | 类型 | 长度 | 小数位 | 导出 |
|---:|---|---|---|---:|---:|---|
| 1 | 姓名 | XM | C | 30 | - | √ |
| 2 | 身份证号 | SFZH | C | 18 | - | √ |
| 3 | 年龄 | NL | N | 3 | 0 | √ |
| 4 | 成绩 | CJ | N | 10 | 2 | √ |
| 5 | 出生日期 | CSRQ | D | - | - | √ |

支持用户修改：

- DBF字段名称
- 字段类型
- 字段长度
- 小数位数
- 字段顺序
- 是否导出

支持操作：

```text
新增字段

删除字段

上移

下移

恢复自动识别

全部导出

全部取消
```

---

# 12. Visual FoxPro 字段类型

支持以下字段类型。

| 类型名称 | DBF标识 | 第一版 |
|---|---|---|
| Character | C | 必须 |
| Numeric | N | 必须 |
| Float | F | 支持 |
| Date | D | 必须 |
| DateTime | T | 支持 |
| Logical | L | 必须 |
| Integer | I | 支持 |
| Double | B | 支持 |

暂不支持：

| 类型 | 原因 |
|---|---|
| Memo | 不处理FPT |
| General | 不适合Excel转换 |
| Picture | 不处理二进制文件 |
| Currency | 第一版暂不支持 |

---

# 13. 字段名称规则

DBF字段名称必须进行校验。

建议规则：

```text
最大长度：10字符

允许：

字母
数字
下划线

不允许：

空格
特殊字符

不能以数字开头
```

如果用户输入：

```text
姓名
```

系统可以提示：

```text
建议使用英文或拼音字段名称。
```

如果用户输入不符合规范：

```text
123NAME
```

提示：

```text
字段名称不能以数字开头。
```

如果字段重复：

```text
NAME
NAME
```

提示：

```text
DBF字段名称重复。
```

---

# 14. 字段长度自动计算

Character 字段：

系统读取所有数据或抽样数据。

例如：

```text
张三
李四
中华人民共和国
```

最长长度：

```text
6
```

建议：

```text
C(10)
```

可以采用：

```text
最大长度 + 缓冲长度
```

默认：

```text
最大长度 + 5
```

最大不能超过 DBF 支持范围。

用户可以手动修改。

---

# 15. 数据校验

转换前必须进行数据校验。

校验项目：

---

## 15.1 字段名称校验

检查：

- 是否为空。
- 是否重复。
- 是否超过最大长度。
- 是否包含非法字符。

---

## 15.2 Numeric 数据校验

例如：

```text
字段类型：

N(3,0)
```

数据：

```text
1234
```

提示：

```text
数据长度超过字段限制。
```

如果：

```text
ABC
```

提示：

```text
无法转换为数字。
```

---

## 15.3 Date 数据校验

例如：

```text
2026-15-99
```

提示：

```text
日期格式错误。
```

---

## 15.4 Logical 数据校验

支持转换：

```text
是 → T
否 → F

True → T
False → F

1 → T
0 → F
```

无法识别的数据提示错误。

---

# 16. 错误处理方式

用户可以选择：

```text
遇到数据错误：

○ 立即停止转换

○ 跳过错误行

○ 错误字段置空
```

默认：

```text
立即停止转换
```

---

# 17. 转换错误报告

如果转换过程中存在错误数据。

生成错误报告。

例如：

| 行号 | 字段名称 | 原始数据 | 错误原因 |
|---:|---|---|---|
| 10 | 年龄 | ABC | 无法转换为数字 |
| 25 | 出生日期 | 2026-99-99 | 日期格式错误 |

错误报告支持：

```text
Excel格式
```

例如：

```text
转换错误报告.xlsx
```

用户可以：

```text
查看错误

导出错误报告

打开错误文件所在目录
```

---

# 18. DBF 转 Excel

操作流程：

```text
选择DBF文件
      ↓
读取DBF结构
      ↓
读取数据
      ↓
数据预览
      ↓
设置Excel导出选项
      ↓
设置Sheet名称
      ↓
选择输出路径
      ↓
开始转换
      ↓
生成Excel
```

---

# 19. DBF 文件读取

用户选择：

```text
student.dbf
```

系统读取：

```text
字段名称
字段类型
字段长度
小数位数
数据记录
```

显示 DBF 结构：

| 字段 | 类型 | 长度 | 小数位 |
|---|---|---:|---:|
| XM | C | 30 | |
| SFZH | C | 18 | |
| NL | N | 3 | 0 |
| CSRQ | D | | |

---

# 20. DBF 数据预览

显示：

| XM | SFZH | NL | CSRQ |
|---|---|---:|---|
| 张三 | 140101... | 20 | 2000-01-01 |
| 李四 | 140102... | 21 | 1999-02-02 |

支持：

- 前100条预览。
- 横向滚动。
- 数据行数显示。
- 字段数量显示。

---

# 21. DBF 字符编码

DBF 中文读取必须考虑编码问题。

支持：

```text
自动识别

GBK

GB2312

UTF-8

ANSI
```

默认：

```text
自动识别
```

如果预览乱码，用户可以切换：

```text
当前编码：

[GBK ▼]

[重新加载]
```

切换编码后重新读取 DBF 数据。

---

# 22. DBF 转 Excel 配置

用户需要配置：

```text
Excel文件格式：

○ XLSX
○ XLS
```

默认：

```text
XLSX
```

---

## 22.1 Sheet 名称

支持自定义：

```text
Sheet名称：

[数据导出]
```

规则：

- 不能为空。
- 最大31个字符。
- 不能包含非法字符。

非法字符：

```text
\
/
?
*
[
]
:
```

---

# 23. 大数据量自动拆分 Sheet

Excel 单个 Sheet 存在行数限制。

因此需要自动拆分。

例如：

DBF：

```text
2,000,000条数据
```

转换：

```text
数据_1

数据_2
```

默认规则：

XLSX：

```text
最大1,048,576行
```

XLS：

```text
最大65,536行
```

建议预留：

```text
最大行数配置
```

例如：

```text
每个Sheet最大：

[500000]
```

转换后：

```text
学生数据_1

学生数据_2

学生数据_3
```

---

# 24. 批量转换

支持两种模式。

---

## 24.1 多文件选择

例如：

```text
选择文件：

student1.xlsx
student2.xlsx
student3.xlsx
```

批量转换。

---

## 24.2 文件夹批量转换

用户选择：

```text
源目录：

D:\ExcelFiles
```

输出目录：

```text
D:\Output
```

自动扫描：

```text
.xlsx

.xls
```

进行批量转换。

DBF 转 Excel同理。

---

# 25. 批量转换配置

支持：

```text
源目录

输出目录

转换类型

是否包含子目录

文件重名处理

错误处理方式
```

---

## 25.1 文件重名处理

支持：

```text
○ 覆盖

○ 自动重命名

○ 跳过
```

默认：

```text
自动重命名
```

例如：

```text
student.dbf

student_1.dbf

student_2.dbf
```

---

# 26. 批量转换结果

转换完成显示：

| 文件名称 | 状态 | 信息 |
|---|---|---|
| student1.xlsx | 成功 | 转换完成 |
| student2.xlsx | 成功 | 转换完成 |
| student3.xlsx | 失败 | 数据格式错误 |

统计：

```text
总文件：100

成功：95

失败：5

耗时：00:02:30
```

---

# 27. 转换进度

转换过程中显示进度。

例如：

```text
正在转换：

student.xlsx

████████████████░░░░░

80%

80000 / 100000
```

显示：

- 当前文件。
- 当前处理行数。
- 总行数。
- 百分比。
- 已耗时。
- 预计剩余时间。

支持：

```text
取消转换
```

第一版不要求暂停。

---

# 28. 模板系统

模板是本软件的重要功能。

用户可以保存 Excel → DBF 字段配置。

---

# 29. 模板创建

用户可以创建：

```text
模板名称：

学生信息导入模板

模板描述：

学生基本信息Excel转换DBF格式
```

字段配置：

| Excel字段 | DBF字段 | 类型 | 长度 | 小数位 |
|---|---|---|---:|---:|
| 姓名 | XM | C | 30 | |
| 身份证号 | SFZH | C | 18 | |
| 性别 | XB | C | 2 | |
| 出生日期 | CSRQ | D | | |
| 年龄 | NL | N | 3 | 0 |

保存模板。

---

# 30. 模板使用

Excel 转 DBF 时：

```text
选择模板：

[不使用模板 ▼]

学生信息模板

人员信息模板

成绩信息模板
```

选择模板后：

自动应用：

```text
DBF字段名称

字段类型

字段长度

小数位

字段顺序
```

---

# 31. 模板匹配

模板默认采用：

```text
Excel列名称精确匹配
```

例如模板：

```text
姓名
身份证号
出生日期
```

Excel：

```text
姓名
身份证号
出生日期
```

自动匹配。

如果 Excel 缺少字段：

提示：

```text
Excel中未找到字段：

出生日期
```

如果 Excel 多余字段：

提示：

```text
以下字段未使用：

联系电话
家庭住址
```

用户可以选择继续。

---

# 32. 模板管理

模板管理页面支持：

```text
新建模板

编辑模板

复制模板

删除模板

导入模板

导出模板
```

模板列表：

| 模板名称 | 描述 | 创建时间 | 操作 |
|---|---|---|---|
| 学生信息 | 学生基础数据 | 2026-09-04 | 编辑 删除 |
| 人员信息 | 人员数据 | 2026-09-04 | 编辑 删除 |

---

# 33. 模板导入导出

模板采用 JSON 文件。

扩展名建议：

```text
.exdbftemplate
```

本质：

```text
JSON
```

例如：

```json
{
  "name": "学生信息模板",
  "description": "学生基础信息导入模板",
  "version": "1.0",
  "fields": [
    {
      "excelColumnName": "姓名",
      "dbfFieldName": "XM",
      "dbfFieldType": "C",
      "length": 30,
      "decimalCount": 0,
      "required": true,
      "sortOrder": 1
    },
    {
      "excelColumnName": "身份证号",
      "dbfFieldName": "SFZH",
      "dbfFieldType": "C",
      "length": 18,
      "decimalCount": 0,
      "required": true,
      "sortOrder": 2
    }
  ]
}
```

支持：

```text
导出模板

student.exdbftemplate
```

其他用户可以：

```text
导入模板
```

实现模板共享。

---

# 34. 转换历史

本地记录转换历史。

记录：

```text
转换时间

源文件名称

源文件路径

目标文件名称

目标文件路径

转换类型

转换状态

转换耗时

成功数量

失败数量
```

历史列表：

| 时间 | 文件 | 类型 | 状态 |
|---|---|---|---|
| 2026-09-04 10:20 | student.xlsx | Excel→DBF | 成功 |
| 2026-09-04 09:30 | student.dbf | DBF→Excel | 成功 |

支持：

```text
重新打开文件

打开文件目录

删除记录

清空历史
```

---

# 35. 系统设置

设置页面包括：

---

## 35.1 默认输出目录

```text
默认输出目录：

D:\ExcelDBFConverter\Output
```

支持：

```text
选择目录
```

---

## 35.2 默认 Excel 格式

```text
○ XLSX

○ XLS
```

默认：

```text
XLSX
```

---

## 35.3 默认 DBF 编码

```text
自动

GBK

GB2312

UTF-8

ANSI
```

---

## 35.4 数据预览数量

```text
100

500

1000
```

默认：

```text
100
```

---

## 35.5 自动文本识别关键词

默认：

```text
编号
代码
号码
身份证
电话
手机
学号
考生号
邮编
账号
卡号
```

用户可以：

```text
新增

删除

恢复默认
```

---

# 36. 本地数据存储

软件需要保存：

```text
转换历史

系统配置

模板信息
```

建议使用：

```text
SQLite
```

数据库：

```text
ExcelDBFConverter.db
```

---

# 37. 数据库表设计

## 37.1 Template

```text
Id

Name

Description

Version

CreatedTime

UpdatedTime
```

---

## 37.2 TemplateField

```text
Id

TemplateId

ExcelColumnName

DbfFieldName

DbfFieldType

FieldLength

DecimalCount

IsRequired

SortOrder
```

---

## 37.3 ConversionHistory

```text
Id

ConversionType

SourceFileName

SourceFilePath

TargetFileName

TargetFilePath

Status

TotalRows

SuccessRows

FailedRows

ElapsedMilliseconds

ErrorMessage

CreatedTime
```

---

## 37.4 AppSetting

```text
Id

SettingKey

SettingValue

UpdatedTime
```

---

# 38. 技术架构

推荐：

```text
.NET 8
+
WPF
+
MVVM
```

---

## 38.1 UI框架

使用：

```text
WPF
```

MVVM：

```text
CommunityToolkit.Mvvm
```

---

## 38.2 依赖注入

使用：

```text
Microsoft.Extensions.DependencyInjection
```

---

## 38.3 Excel处理

推荐：

```text
NPOI
```

用于：

```text
.xls

.xlsx
```

要求：

- 不依赖 Microsoft Office。
- 支持读取。
- 支持写入。
- 支持大文件。

---

## 38.4 DBF处理

需要实现：

```text
Visual FoxPro DBF Reader

Visual FoxPro DBF Writer
```

支持字段：

```text
C
N
F
D
T
L
I
B
```

建议单独封装接口。

```csharp
public interface IDbfReader
{
    Task<DbfTableInfo> ReadStructureAsync(string filePath);

    IAsyncEnumerable<DbfRecord> ReadRecordsAsync(
        string filePath,
        DbfReadOptions options);
}
```

```csharp
public interface IDbfWriter
{
    Task CreateAsync(
        string filePath,
        DbfTableSchema schema);

    Task WriteRecordAsync(
        DbfRecord record);
}
```

禁止 DBF 读取逻辑直接写入 ViewModel。

---

# 39. CSV

第一版本明确：

```text
不支持CSV。
```

不提供：

```text
CSV → DBF

DBF → CSV

CSV导入
```

后续如有需求再增加。

---

# 40. 项目目录结构

推荐采用：

```text
ExcelDbfConverter
│
├── src
│
│   ├── ExcelDbfConverter.Desktop
│   │   │
│   │   ├── Views
│   │   │   ├── MainWindow
│   │   │   ├── ExcelToDbf
│   │   │   ├── DbfToExcel
│   │   │   ├── BatchConversion
│   │   │   ├── Template
│   │   │   ├── History
│   │   │   └── Settings
│   │   │
│   │   ├── ViewModels
│   │   │
│   │   ├── Controls
│   │   │
│   │   ├── Converters
│   │   │
│   │   └── Resources
│   │
│   ├── ExcelDbfConverter.Core
│   │   │
│   │   ├── Models
│   │   ├── Enums
│   │   ├── DTOs
│   │   ├── Interfaces
│   │   └── Constants
│   │
│   ├── ExcelDbfConverter.Application
│   │   │
│   │   ├── Services
│   │   ├── Converters
│   │   ├── Validators
│   │   ├── Templates
│   │   └── History
│   │
│   └── ExcelDbfConverter.Infrastructure
│       │
│       ├── Excel
│       ├── Dbf
│       ├── SQLite
│       ├── Logging
│       └── Configuration
│
├── tests
│
│   ├── ExcelDbfConverter.Core.Tests
│   │
│   ├── ExcelDbfConverter.Application.Tests
│   │
│   └── ExcelDbfConverter.Infrastructure.Tests
│
├── docs
│
└── README.md
```

---

# 41. 大文件处理要求

禁止：

```text
一次性读取全部数据

全部加载DataTable

全部加载ObservableCollection
```

应该采用：

```text
流式读取

分批处理

异步写入
```

Excel：

```text
读取 → 转换 → 写入
```

DBF：

```text
读取一条

↓

转换一条

↓

写入一条
```

避免：

```text
100万条数据全部进入内存。
```

---

# 42. 转换任务设计

建议抽象转换任务。

```csharp
public interface IConversionTask
{
    Task<ConversionResult> ExecuteAsync(
        ConversionRequest request,
        IProgress<ConversionProgress> progress,
        CancellationToken cancellationToken);
}
```

支持：

```text
ExcelToDbfConversionTask

DbfToExcelConversionTask
```

---

# 43. 转换进度模型

```csharp
public class ConversionProgress
{
    public string CurrentFileName { get; set; }

    public long CurrentRow { get; set; }

    public long TotalRows { get; set; }

    public double Percentage { get; set; }

    public TimeSpan Elapsed { get; set; }

    public TimeSpan? EstimatedRemaining { get; set; }
}
```

---

# 44. 日志系统

使用：

```text
Serilog
```

日志目录：

```text
logs
```

日志文件：

```text
2026-09-04.log
```

记录：

```text
程序启动

文件读取

转换开始

转换结束

异常信息

错误详情
```

---

# 45. 异常处理

所有异常统一处理。

例如：

```text
文件不存在

文件被占用

Excel格式错误

DBF格式错误

字段类型错误

字段名称错误

编码错误

磁盘空间不足

写入失败
```

UI 不直接显示：

```text
StackTrace
```

用户看到：

```text
转换失败：

无法读取DBF文件。

可能原因：

1. 文件格式不是Visual FoxPro DBF。
2. 文件已经损坏。
3. 文件正在被其他程序占用。

详细错误信息请查看日志。
```

---

# 46. UI设计原则

界面要求：

```text
简洁

现代

工具软件风格

不要复杂

不要过度动画
```

建议：

- 左侧菜单。
- 顶部标题栏。
- 主工作区域。
- 浅色主题。
- 支持 Windows 深色模式可作为后续版本。

推荐页面：

```text
┌────────────┬─────────────────────────────┐
│            │                             │
│ 首页       │                             │
│            │       主内容区域             │
│ Excel→DBF  │                             │
│            │                             │
│ DBF→Excel  │                             │
│            │                             │
│ 批量转换   │                             │
│            │                             │
│ 模板管理   │                             │
│            │                             │
│ 转换历史   │                             │
│            │                             │
│ 设置       │                             │
│            │                             │
└────────────┴─────────────────────────────┘
```

---

# 47. MVP第一版本范围

第一版本必须完成：

## 核心转换

- Excel → DBF
- DBF → Excel

## Excel

- XLS
- XLSX
- Sheet选择
- 数据预览

## DBF

- Visual FoxPro DBF
- DBF字段读取
- DBF数据读取

## 字段

- 自动识别
- 手动修改
- 字段长度设置
- 小数位设置

## 类型

```text
C
N
F
D
T
L
I
B
```

## 模板

- 创建
- 编辑
- 删除
- 应用
- 导入
- 导出

## 批量

- 多文件
- 文件夹

## 其他

- 转换进度
- 转换取消
- 错误报告
- 转换历史
- 系统设置

---

# 48. 后续版本扩展

后续可以考虑：

## V2

```text
CSV支持

DBF Memo支持

FPT支持

转换规则高级配置

字段默认值

数据格式转换规则

正则表达式校验

数据清洗
```

---

## V3

```text
DBF文件修复

DBF文件结构编辑

DBF字段新增

DBF字段删除

DBF数据编辑

DBF索引支持

数据对比

Excel字段智能匹配
```

---

# 49. 软件发布要求

使用：

```text
Self-contained
```

发布。

目标：

```text
win-x64
```

要求：

用户电脑无需安装：

```text
.NET Runtime
```

发布后目录：

```text
ExcelDBFConverter
│
├── ExcelDBFConverter.exe
├── ExcelDBFConverter.dll
├── runtimes
├── data
├── templates
├── logs
└── config
```

最终压缩：

```text
ExcelDBFConverter_v1.0.0_win-x64.zip
```

用户：

```text
下载

↓

解压

↓

双击ExcelDBFConverter.exe

↓

运行
```

---

# 50. 开发优先级

## 第一阶段

基础项目：

```text
WPF项目

MVVM

导航

依赖注入

日志

SQLite
```

---

## 第二阶段

Excel模块：

```text
Excel读取

Sheet读取

表头读取

数据预览

字段识别
```

---

## 第三阶段

DBF模块：

```text
DBF结构读取

DBF数据读取

DBF文件创建

DBF数据写入
```

这是整个项目最核心的技术难点。

---

## 第四阶段

转换模块：

```text
Excel → DBF

DBF → Excel

进度显示

取消任务

错误处理
```

---

## 第五阶段

模板模块：

```text
模板CRUD

模板应用

模板导入

模板导出
```

---

## 第六阶段

批量转换：

```text
多文件

文件夹

转换队列

结果统计
```

---

## 第七阶段

完善：

```text
错误报告

历史记录

系统设置

性能优化

发布
```

---

# 51. 开发原则

AI 或开发人员必须遵守：

1. 不要将业务逻辑写在 View 中。
2. 不要将文件读取逻辑直接写在 ViewModel 中。
3. 所有转换逻辑必须通过 Service。
4. DBF Reader 和 Writer 必须独立封装。
5. Excel 和 DBF 逻辑不能耦合。
6. 支持 CancellationToken。
7. 大文件不能全部加载内存。
8. 所有 IO 操作使用异步方式。
9. 所有异常统一处理。
10. UI线程不能执行耗时转换任务。
11. 必须考虑文件被占用情况。
12. 必须考虑编码问题。
13. 必须考虑转换过程中用户取消操作。
14. 必须保证转换失败不会产生损坏的目标文件。
15. 转换完成后再将临时文件重命名为正式文件。

---

# 52. 最终产品目标

最终用户操作应该足够简单。

Excel 转 DBF：

```text
打开软件

↓

选择Excel

↓

选择Sheet

↓

确认字段

↓

选择输出位置

↓

点击转换

↓

完成
```

DBF 转 Excel：

```text
打开软件

↓

选择DBF

↓

确认数据

↓

设置Sheet名称

↓

选择输出位置

↓

点击转换

↓

完成
```

对于高级用户：

```text
可以使用模板

可以批量转换

可以修改字段规则

可以配置数据校验
```

---

# 53. 项目最终目标

ExcelDBF Converter 应该是一款：

```text
轻量

稳定

离线

安全

简单

专业
```

的数据格式转换工具。

核心竞争力：

```text
Excel ↔ Visual FoxPro DBF 双向转换

字段自动识别

字段自定义映射

模板复用

批量处理

完全本地离线

绿色免安装
```

---

# 54. AI开发执行建议

如果使用 Claude Code 或 Codex 或 deepseek harness开发，建议不要一次性要求 AI 完成整个项目。

推荐开发顺序：

```text
Step 1
创建项目基础架构

↓

Step 2
完成WPF主界面和导航

↓

Step 3
实现Excel读取

↓

Step 4
实现Excel数据预览

↓

Step 5
实现字段自动识别

↓

Step 6
实现Visual FoxPro DBF Reader

↓

Step 7
实现Visual FoxPro DBF Writer

↓

Step 8
实现Excel → DBF

↓

Step 9
实现DBF → Excel

↓

Step 10
实现模板系统

↓

Step 11
实现批量转换

↓

Step 12
性能优化和测试

↓

Step 13
绿色版本发布
```

每完成一个阶段必须：

```text
编译

运行

测试

确认

再进入下一阶段。
```

禁止在没有测试核心 DBF 读写功能的情况下直接堆积 UI 功能。

因为：

> DBF 兼容性才是这个项目真正的技术核心。