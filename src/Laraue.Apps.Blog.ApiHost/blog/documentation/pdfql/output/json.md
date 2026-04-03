---
type: documentation
title: JSON
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
The data from the pipeline returns as is.

All examples work with the next PDF structure

#### --- PDF start ---

|                   |                   |
|-------------------|-------------------|
| Table1 Row1 Cell1 | Table1 Row1 Cell2 |
| Table1 Row2 Cell1 | Table1 Row2 Cell2 |  

|                   |                   |
|-------------------|-------------------|
| Table2 Row1 Cell1 | Table2 Row1 Cell2 |
| Table2 Row2 Cell1 | Table2 Row2 Cell2 |

#### --- PDF end ---


### Examples
1. Return all tables from document
```csharp
select(tables) // PdfTable[]
```
Will produce the next output  

```json
[
  [
   [
    "Table1 Row1 Cell1",
    "Table1 Row1 Cell2"
   ],
   [
    "Table1 Row2 Cell1",
    "Table1 Row2 Cell2"
   ]
  ],
  [
   [
    "Table2 Row1 Cell1",
    "Table2 Row1 Cell1"
   ],
   [
    "Table2 Row2 Cell1",
    "Table2 Row2 Cell2"
   ]
  ]
]
```