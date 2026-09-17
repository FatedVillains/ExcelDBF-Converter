# Visual FoxPro DBF (.dbf) Binary Format — Byte-Level Reference

**Scope:** the on-disk format of a Visual FoxPro **table** (.dbf, header version byte `0x30`), as produced by VFP 3.0–9.0 (SP2). Covers everything needed to implement a reader/writer in C# whose output opens correctly in VFP and Excel.

> **Verification method:** every claim below was cross-checked against (a) Microsoft's official VFP documentation, (b) the source of the two most widely used open-source implementations — the Python [`dbf`](https://github.com/ethanfurman/dbf) package (Ethan Furman) and [`dbfread`](https://github.com/olemb/dbfread) (Ole Martin Bjørndalen) — and (c) the maintained .NET Core reader [`DbfDataReader`](https://github.com/yellowfeather/DbfDataReader), plus (d) **byte-for-byte inspection of two real VFP `0x30` files** (`memotest.dbf` from dbfread's test suite and `dbase_30.dbf` from DbfDataReader's fixtures; the latter has 145 columns including `B`/`T` fields with known values validated against a CSV).

---

## 0. Byte Order / Endianness Summary

The single most contested facts (T field layout and B field byte order) are resolved here with citations and **empirical proof** (§3.5, §3.8).

| Field type | On-disk encoding | Byte order on disk | Confirmed by |
|---|---|---|---|
| **T** (DateTime) | 8 bytes = two 32-bit ints: **dword 0 = Julian Day Number**, **dword 1 = milliseconds since midnight** | both dwords **little-endian**; date dword first (bytes 0–3), time dword last (bytes 4–7) | dbfread `struct.unpack('<LL', data)`; dbf `update_integer` (`'<i'` for both halves); DbfDataReader `BitConverter.ToInt32(bytes)` then `ToInt32(bytes[4..])`; manmrk tutorial; SO 25506256 quote |
| **I** (Integer) | 4-byte signed 32-bit | **little-endian** | dbfread `'<i'`; dbf `struct.pack('<i')` |
| **B** (Double) | 8-byte IEEE-754 binary64 | **little-endian — NOT reversed.** Real file: value 8.00 stored as `00 00 00 00 00 00 20 40`; 50.00 as `00 00 00 00 00 00 49 40`; 20.00 as `00 00 00 00 00 00 34 40` | dbfread `struct.unpack('d', …)` (native LE); dbf `struct.pack('<d', …)`; DbfDataReader `BitConverter.ToDouble(bytes)`; **byte inspection of dbase_30.dbf** (values match the fixture CSV exactly) |
| **Y** (Currency) | 8-byte signed 64-bit integer scaled ×10,000 | **little-endian** | dbfread `'<q' / 10000`; dbf `struct.pack('<q', value*10000)` |
| Header counters (record count, header size, record size, field displacement) | 32-bit / 16-bit integers | **little-endian** | MS Learn; dbfread header struct; dbf `pack_short_int`/`pack_long_int` |

> ⚠️ **On the widely repeated claim "VFP stores doubles in reversed byte order":** this is **false for the `B` field** and almost certainly originates from the *CDX index key* encoding, where doubles are byte-swapped (and bit-tricked) so keys sort with `memcmp` — see the [xBase file-format description](https://cdn.jsdelivr.net/npm/dbffile-fix-decoding@1.8.1/doc/xbase-file-format-description.html) CDX section ("Numbers are stored as IEEE doubles … Swap the order of the bytes …"). Index-key encoding is unrelated to `B` field storage. All three mainstream implementations read/write `B` as plain little-endian, and a real VFP file proves it. If a C# library you encounter reverses bytes before `BitConverter.ToDouble`, it is compensating for its own earlier assumption, not for the file format.

---

## 1. HEADER LAYOUT (32 bytes + field descriptors + 0x0D + 263-byte backlink)

Sources: [Microsoft Learn: "Table File Structure (.dbc, .dbf, …)"](https://learn.microsoft.com/en-us/previous-versions/visualstudio/foxpro/aa975386(v=vs.71)) (official, authoritative), [dbfread `dbf.py` header struct](https://github.com/olemb/dbfread/blob/master/dbfread/dbf.py) (`<BBBBLHHHBBLLLBBH`), and [Erik Bachmann's "Xbase File Format Description"](https://cdn.jsdelivr.net/npm/dbffile-fix-decoding@1.8.1/doc/xbase-file-format-description.html).

| Byte offset | Size | Meaning |
|---|---|---|
| 0 | 1 | **Version / file type byte** — `0x30` = Visual FoxPro (see §6) |
| 1–3 | 3 | **Last update date: YY MM DD** (binary). Year is a 2-digit byte; readers use a pivot — dbfread: `< 80 → 20xx, ≥ 80 → 19xx` ([`expand_year`](https://github.com/olemb/dbfread/blob/master/dbfread/dbf.py)). Bachmann's doc notes the byte is "year − 1900" giving range 1900–2155. |
| 4–7 | 4 | **Number of records in file** (32-bit, little-endian) |
| 8–9 | 2 | **Header size** = "position of first data record" (16-bit, little-endian). **Always read this; never hardcode it** (see §7). |
| 10–11 | 2 | **Length of one data record including the delete flag** (16-bit, little-endian) = 1 + Σ field lengths |
| 12–27 | 16 | Reserved. (Classic dBASE layout places an "incomplete transaction" flag at 14 and "encryption" flag at 15; dbfread parses them at those offsets. VFP writes 0x00 here.) |
| 28 (0x1C) | 1 | **Table flags / MDX flag** (MS Learn calls it "Table flags"): `0x01` = has structural `.cdx`; `0x02` = has memo field; `0x04` = file is a database container (.dbc). Values combine (e.g. `0x03` = .cdx + memo). |
| 29 (0x1D) | 1 | **Code page mark / language driver** (see §4) |
| 30–31 | 2 | Reserved (0x00) |
| 32 … n | 32×N | **Field subrecords** — one 32-byte descriptor per field (§2) |
| n+1 | 1 | **Header terminator `0x0D`** |
| n+2 … n+264 | 263 | **Backlink block** — relative path of the associated database container (.dbc). **If the first byte is `0x00`, the table is not in a database.** (Database files always contain `0x00`.) |

**Header-size formula (VFP):** `headerSize = 32 + 32·fieldCount + 1 (0x0D) + 263 (backlink) = 296 + 32·fieldCount`. Microsoft documents the inverse: *"number of fields = (x − 296) / 32, where x is the position of the first record (bytes 8–9)"* ([MS Learn](https://learn.microsoft.com/en-us/previous-versions/visualstudio/foxpro/aa975386(v=vs.71))). Bachmann's doc confirms: *"(Visual FoxPro) Database Container (DBC) 263 bytes for backlink. Included in header structure."*

**Empirical confirmation (real file `dbase_30.dbf`, VFP `0x30`):** header length field = 4936 = 296 + 32×145 (145 columns) ✓; record length = 3903 = 1 + Σ field lengths ✓; byte 28 = 0x02 (has memo; a matching `.fpt` exists) ✓; byte 29 = 0x03 (cp1252) ✓; file size 137,639 = 4936 + 34×3903 + 1 (EOF `0x1A`) ✓. Second file `memotest.dbf`: version `0x30`, 3 records, header 0x0188 = 392 = 296 + 32×3 ✓, record size 0x1D = 29 = 1 + 16 + 8 + 4 ✓, byte 28 = 0x02 ✓.

## 2. FIELD DESCRIPTOR (32 bytes each)

Sources: [MS Learn "Field Subrecords Structure"](https://learn.microsoft.com/en-us/previous-versions/visualstudio/foxpro/aa975386(v=vs.71)) (authoritative) and dbfread's [`DBFField` struct](https://github.com/olemb/dbfread/blob/master/dbfread/dbf.py) (`<11scLBBHBBBB7sB`).

| Byte offset | Size | Meaning |
|---|---|---|
| 0–10 | 11 | **Field name** — max **10 characters**, **padded with 0x00 (NUL)** when shorter |
| 11 | 1 | **Field type** — `C Y N F D T B I L M G P` … (see §3) |
| 12–15 | 4 | **Displacement (address) of field within record** — 32-bit **little-endian**. The **first field has displacement 1** (byte 0 of each record is the delete flag; fields start at byte 1). Bachmann: "offset of field from beginning of record". |
| 16 | 1 | **Field length in bytes** |
| 17 | 1 | **Number of decimal places** |
| 18 | 1 | **Field flags:** `0x01` system column (hidden), `0x02` column can store null, `0x04` binary column (CHAR/MEMO), `0x06` = NULL+binary, `0x0C` autoincrementing |
| 19–22 | 4 | Autoincrement "Next value" |
| 23 | 1 | Autoincrement "Step value" |
| 24–31 | 8 | Reserved |

Confirmed against real `dbase_30.dbf`: `ACCESSNO` → type `C`, displacement `01 00 00 00` (1), length 15; `ACQVALUE` → type `B`, displacement `10 00 00 00` (16), length 8, decimals 2; `FLAGDATE` → type `T`, displacement 799, length 8; `APPNOTES` → type `M`, displacement 24, length 4.

## 3. FIELD TYPES AND ON-DISK STORAGE

Sources: dbfread [`field_parser.py`](https://github.com/olemb/dbfread/blob/master/dbfread/field_parser.py), the dbf package [`tables.py`](https://github.com/ethanfurman/dbf/blob/master/dbf/tables.py), DbfDataReader [`DbfValueDateTime.cs`](https://github.com/yellowfeather/DbfDataReader/blob/main/src/DbfDataReader/DbfValueDateTime.cs)/[`DbfValueDouble.cs`](https://github.com/yellowfeather/DbfDataReader/blob/main/src/DbfDataReader/DbfValueDouble.cs), and MS Learn. (VFP also supports `Y` Currency, `M/G/P` memo pointers, `V` varchar, `Q` varbinary, `W` blob in VFP 9.)

### 3.1 C — Character
ASCII/ANSI text, **padded to the full field length with spaces (0x20)**. Stored length counts **bytes**: under GBK (code page 936) a Chinese character is **2 bytes**, so a `C(10)` field holds **5** Chinese characters (10 bytes). Readers decode with the table's code page and typically trim trailing spaces/NULs (dbfread `parseC` does `rstrip(b'\0 ')`). Empty = all spaces.

### 3.2 N — Numeric
ASCII decimal text, **right-justified**, **padded with spaces** to field length. The field length **includes the sign and the decimal point**; `decimal_count` (descriptor byte 17) is the digits after the point. Negative numbers use a leading `-`; the decimal separator is `.` on disk. The dbf package writes with C-style `"%*.*f" % (length, decimals, value)` (right-justified, space-padded). Empty = all spaces. Example: `N(10,2)` value −12345.67 → `"  -12345.67"` (10 bytes).

### 3.3 F — Float
**Identical on-disk representation to N**: ASCII, right-justified, space-padded (dbfread `parseF` ≡ `parseN`; the dbf package maps both FLOAT and NUMERIC to `retrieve_numeric`/`update_numeric`). Empty = all spaces.

### 3.4 D — Date
Exactly **8 bytes of ASCII `"YYYYMMDD"`** (e.g. `19870301`). **Empty date = 8 spaces (0x20×8)**; some writers use `"00000000"` — both major libraries treat either as empty (dbfread `parseD`: `data.strip(b' 0\0') == b'' → None`; the dbf package checks `text in (b'        ', b'00000000')`). For VFP compatibility write spaces.

### 3.5 T — DateTime ⭐ (epoch + byte order — verified)
8 bytes = **two 32-bit integers**, **both little-endian**:

* **dword 0 (bytes 0–3): Julian Day Number (JDN)** — the **astronomical** Julian day, where **day 0 = January 1, 4713 BC (proleptic Julian calendar)**. Concretely, implementations convert as `stored = python_ordinal + 1_721_425` (dbfread: `datetime.fromordinal(day − 1_721_425)`; dbf package: `moment.toordinal() + VFPTIME`, `VFPTIME = 1_721_425`; DbfDataReader: `new DateTime(1,1,1).AddDays(jdn − 1_721_426)`). Sanity check: 2000-01-01 → JDN **2451545**, which round-trips through all three formulas. The user-facing quote in [SO 25506256](https://stackoverflow.com/questions/25506256/convert-java-date-to-dbf-foxpro-datetime-format) ("days from 1/1/4712BC") and the [manmrk xBase tutorial](http://www.manmrk.net/tutorials/database/xbase/data_types.html) ("days since January 1st, 4713 BC") agree.
* **dword 1 (bytes 4–7): milliseconds since midnight**, range **0 … 86,399,999** (dbfread builds `timedelta(seconds=msec/1000)`; the dbf package writes `((h*3600 + m*60 + s)*1000 + millis)`).

**Empty T = 8 × 0x00** (both dwords 0). Readers return null/empty when dword 0 == 0 (dbfread also treats an all-space field as empty).

**.NET ↔ VFP conversion (exact):**
```csharp
// Write (DateTime dt → 8 bytes)
long jdn = (dt.Date - new DateTime(1, 1, 1)).Days + 1721426L;   // JDN
int  ms  = (int)dt.TimeOfDay.TotalMilliseconds;                 // 0..86,399,999
byte[] bytes = new byte[8];
bytes[0..4]  = BitConverter.GetBytes((int)jdn);   // little-endian
bytes[4..8]  = BitConverter.GetBytes(ms);         // little-endian
// empty datetime → all 8 bytes = 0x00

// Read (8 bytes → DateTime)
long jdn2 = BitConverter.ToInt32(bytes, 0);
int  ms2  = BitConverter.ToInt32(bytes, 4);
if (jdn2 == 0) return null;                                     // empty
var value = new DateTime(1, 1, 1).AddDays(jdn2 - 1721426L).AddMilliseconds(ms2);
```

### 3.6 L — Logical
**1 byte**, ASCII: `T`, `t`, `Y`, `y` = true; `F`, `f`, `N`, `n` = false; `?` = unknown; ` ` (space) = empty/null (dbfread `parseL`: `b'TtYy'` / `b'FfNn'` / `b'? \0'`).

### 3.7 I — Integer
**4 bytes, signed, little-endian** (dbfread `struct.unpack('<i', …)`; dbf `struct.pack('<i', …)`). Empty = 0x00000000. Range −2,147,483,648 … 2,147,483,647.

### 3.8 B — Double ⭐ (byte order — verified)
**8 bytes, IEEE-754 binary64, little-endian on disk.** **Not** byte-reversed (see §0 for the myth and proof). Empty = 8 × 0x00 (= 0.0). C#: `BitConverter.ToDouble(bytes, 0)` reads it directly; to write, `BitConverter.GetBytes(value)`.

### 3.9 Y — Currency (bonus, common in VFP tables)
**8 bytes, signed little-endian integer scaled ×10,000** (dbfread `struct.unpack('<q', …) / 10000`; dbf `struct.pack('<q', value*10000)`). Value 20.00 → integer 200000.

### 3.10 M / G / P — Memo pointers
**4 bytes, little-endian block number** into the companion `.fpt` memo file (dbfread `struct.unpack('<I', …)`). Empty memo = `00 00 00 00`.

## 4. CODE PAGE MARK — byte 29 (offset 0x1D)

Authoritative mapping: [MS Learn "Code Pages Supported by Visual FoxPro"](https://learn.microsoft.com/en-us/previous-versions/visualstudio/foxpro/aa975345(v=vs.71)) and [KB Q129631 "Understanding Code Pages in Visual FoxPro"](https://jeffpar.github.io/kbarchive/kb/129/Q129631/). The Python [`dbf`](https://github.com/ethanfurman/dbf/blob/master/dbf/tables.py) and [dbfread](https://github.com/olemb/dbfread/blob/master/dbfread/codepages.py) code-page tables agree on every value below.

| Code page | Platform | **Byte to write (0x1D)** |
|---|---|---|
| **936 (GBK, Simplified Chinese)** | Chinese (PRC, Singapore) Windows | **`0x7A`** |
| **1252** (Windows ANSI / Western) | Windows ANSI | **`0x03`** |
| **437** (US OEM) | U.S. MS-DOS | **`0x01`** |
| 850 | International MS-DOS | 0x02 |
| 852 | Eastern European MS-DOS | 0x64 |
| 866 | Russian MS-DOS | 0x65 |
| 865 | Nordic MS-DOS | 0x66 |
| 861 / 895 / 620 / 737 / 857 | various MS-DOS | 0x67 / 0x68 / 0x69 / 0x6A / 0x6B |
| 932 / 949 / 950 | Japanese / Korean / Traditional Chinese Windows | 0x7B / 0x79 / 0x78 |
| 874 / 1255 / 1256 | Thai / Hebrew / Arabic Windows | 0x7C / 0x7D / 0x7E |
| 1250 / 1251 / 1254 / 1253 | E. European / Russian / Turkish / Greek Windows | 0xC8 / 0xC9 / 0xCA / 0xCB |
| 10000 / 10006 / 10007 / 10029 | Macintosh | 0x04 / 0x98 / 0x96 / 0x97 |

**UTF-8 (65001):** VFP has **no language-driver byte for UTF-8** — it is not in Microsoft's official table. The dbf package uses `0x00 → utf8` only as a reader-side fallback (byte `0x00` officially means "no code page mark"; VFP then prompts for a code page — [KB Q129631](https://jeffpar.github.io/kbarchive/kb/129/Q129631/)) and maps `0xF0` → "8-bit unicode" (a non-VFP/dBASE convention). **Do not** write `0x00` for a GBK table: VFP will prompt, and Excel may fall back to cp1252 and garble Chinese.

**What to write for a GBK (cp936) table so Excel and VFP read Chinese correctly:** write **`0x7A`** at offset 0x1D and store all text as **GBK bytes** (never UTF-8) in `C`/`M` fields. `0x7A` is Microsoft's documented mark for 936 and is exactly what dbfread/dbf decode as cp936. ⚠️ A secondary "language driver" table circulating in some dBASE tooling lists `0x57 = 936`; that value is **not** used by the mainstream implementations (both map `0x57 → cp1252 "ANSI"`), so prefer `0x7A` for new files. (Note: VFP 6/7/8/9 files in the wild exist with several legacy values — `0x7A` is the correct target.)

## 5. RECORD LAYOUT

Source: [MS Learn](https://learn.microsoft.com/en-us/previous-versions/visualstudio/foxpro/aa975386(v=vs.71)) + [Bachmann xBase description](https://cdn.jsdelivr.net/npm/dbffile-fix-decoding@1.8.1/doc/xbase-file-format-description.html) + real-file inspection.

* Each record is `recordLength` bytes (header bytes 10–11).
* Byte 0 = **delete flag**: `0x20` (space) = active record, `0x2A` (`*`) = **deleted** record. (Confirmed in `memotest.dbf`: `20`, `20`, `2A`.)
* Bytes 1 … `recordLength−1` = the field data **packed back-to-back, no separators, no record terminators**, in descriptor order, using each field's displacement/length. The record is exactly `recordLength` bytes (fields sum to `recordLength−1`; no padding beyond that, but **readers should tolerate and writers should produce** exact-length records — Excel/VFP compute record boundaries from the header length).
* **EOF marker: `0x1A`** (26, Ctrl-Z) after the last record. dBASE III appends an extra `0x1A`; physical size may exceed logical size after PACK (trailing garbage is possible — [Bachmann](https://cdn.jsdelivr.net/npm/dbffile-fix-decoding@1.8.1/doc/xbase-file-format-description.html), note *11). Robust readers stop at the first `0x1A` or at EOF (dbfread iterates exactly this way). Write a single trailing `0x1A` for VFP/Excel compatibility.

## 6. VERSION BYTE (header offset 0)

Authoritative table: [MS Learn](https://learn.microsoft.com/en-us/previous-versions/visualstudio/foxpro/aa975386(v=vs.71)); additional entries from the dbf package's [`version_map`](https://github.com/ethanfurman/dbf/blob/master/dbf/tables.py).

| Byte | Meaning |
|---|---|
| `0x02` | FoxBASE |
| `0x03` | FoxBASE+ / dBASE III PLUS, **no memo** |
| `0x83` | FoxBASE+ / dBASE III PLUS, **with memo** |
| `0x30` | **Visual FoxPro** ← target |
| `0x31` | Visual FoxPro, **autoincrement** enabled |
| `0x32` | Visual FoxPro with **VarChar / VarBinary / BLOB** (VFP 9) |
| `0x43` / `0x63` | dBASE IV SQL table files / system files, no memo |
| `0x8B` / `0xCB` | dBASE IV with memo / dBASE IV SQL with memo |
| `0xF5` | **FoxPro 2.x (or earlier) with memo** |
| `0xFB` | FoxBASE |

Reading rule: treat `0x30`/`0x31`/`0x32` as VFP; `0x03`/`0x83`/`0xF5` are FoxPro 2.x / dBASE III (no T/B/Y support); everything else is some dBASE variant.

## 7. Gotchas — reading/writing VFP 6/9 tables with code page 936

1. **Never hardcode the header size as `32 + 32n`.** Read bytes 8–9 (16-bit LE). VFP headers are `296 + 32n` (includes `0x0D` + 263-byte backlink), but files from other tools differ; only the header field is authoritative. Microsoft's own formula uses it: `fields = (x − 296) / 32`.
2. **Record count (bytes 4–7):** VFP maintains it and it is normally accurate for VFP-written files, but **do not trust it blindly** — some third-party writers, interrupted sessions, and PACK-adjacent states leave it stale, and deleted records still count. Robust approach (used by dbfread): iterate records by `headerSize + i·recordLength` and stop at `0x1A`/EOF; optionally cross-check `fileSize == headerSize + count·recordLength + 1`. (Real file check: 137639 = 4936 + 34×3903 + 1 ✓.)
3. **The `0x0D` + backlink nuance:** the header ends with `0x0D`, then **263 bytes** of backlink. If backlink byte 0 is non-zero, the table belongs to a `.dbc` and bytes hold the relative database path. Parsing `headerSize` from bytes 8–9 is what makes this safe; never compute offsets from field count alone.
4. **cp936 specifics:** field lengths are in **bytes**, so GBK Chinese occupies 2 bytes/char (a `C(10)` holds 5 chars); write `0x7A` at 0x1D; encode all text as GBK. Excel opens the DBF and uses the code-page mark to decode — with `0x7A` Chinese reads correctly; with `0x00` or a 1252 mark it garbles. Empty `C` = spaces, empty `D` = spaces, empty `N/F` = spaces, empty `T` = 8×0x00, empty `L` = `'?'`/space.
5. **Nullable columns:** VFP tables with nullable fields get a hidden binary field named `_NullFlags` (type `'0'`, `ceil(nullableCount/8)` bytes) appended as the **last** field descriptor, and the per-field flag byte 18 bit `0x02` marks nullable columns. NULL values are stored as the field's empty bytes plus the corresponding null bit. Account for `_NullFlags` when computing record size and reading records (dbfread returns it as a byte string; see its [field types doc](https://dbfread.readthedocs.io/en/latest/field_types.html)).
6. **EOF `0x1A`:** write one trailing `0x1A`; tolerate missing/extra ones when reading. A trailing `0x1A` is what Excel expects at the physical end of the file.
7. **VFP 9 extras:** version `0x32` tables may contain `V` (varchar), `Q` (varbinary), `W` (blob); their length semantics differ from `C` (length lives elsewhere in the descriptor and data may spill into the memo file). If you only need `0x30` compatibility, reject or skip `0x31`/`0x32` fields you don't understand.
8. **Memos:** a `M`/`G`/`P` field is a 4-byte LE block pointer into the companion `.fpt` (512-byte header; block size in its bytes 6–7). Excel does not render memo contents; VFP does. If the table has any memo field, byte 28 flag `0x02` is set.

---

## Source index

* [MS Learn — Table File Structure (.dbc, .dbf, …) — header/field-descriptor tables, backlink, field-count formula](https://learn.microsoft.com/en-us/previous-versions/visualstudio/foxpro/aa975386(v=vs.71))
* [MS Learn — Code Pages Supported by Visual FoxPro (0x1D values)](https://learn.microsoft.com/en-us/previous-versions/visualstudio/foxpro/aa975345(v=vs.71))
* [KB Q129631 — Understanding Code Pages in Visual FoxPro](https://jeffpar.github.io/kbarchive/kb/129/Q129631/)
* [Python dbf package (Ethan Furman) — tables.py: header build, VFPTIME=1721425, codepage dict, field codecs](https://github.com/ethanfurman/dbf/blob/master/dbf/tables.py)
* [dbfread (Ole Martin Bjørndalen) — dbf.py header struct, field_parser.py (T/I/B/Y/L/D codecs), codepages.py](https://github.com/olemb/dbfread)
* [DbfDataReader (.NET Core, maintained) — DbfValueDateTime.cs (JDN epoch 4713 BC, LE ints), DbfValueDouble.cs (plain LE double)](https://github.com/yellowfeather/DbfDataReader)
* [Erik Bachmann — "Xbase File Format Description" (header, field descriptor, codepage, record/EOF semantics; CDX double-key encoding that caused the reversal myth)](https://cdn.jsdelivr.net/npm/dbffile-fix-decoding@1.8.1/doc/xbase-file-format-description.html)
* [manmrk.net — xBase Data Types (T/@ two-long layout, JDN epoch)](http://www.manmrk.net/tutorials/database/xbase/data_types.html)
* [StackOverflow 25506256 — "Convert java Date to dbf FoxPro datetime format" (T-field layout quote)](https://stackoverflow.com/questions/25506256/convert-java-date-to-dbf-foxpro-datetime-format)
* [dbfread — Field Types doc (B = double in VFP, '_NullFlags', varchar notes)](https://dbfread.readthedocs.io/en/latest/field_types.html)
* Real-file ground truth: dbfread `tests/cases/memotest.dbf` and DbfDataReader `test/fixtures/dbase_30.dbf` (+ `dbase_30.csv`), byte-inspected for this document.
