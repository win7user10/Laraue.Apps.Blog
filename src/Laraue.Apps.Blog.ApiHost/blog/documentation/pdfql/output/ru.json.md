---
type: documentation
title: JSON
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Дата из текущей выборки возвращаяется, как есть, сериализуясь в JSON.

Все примеры ниже работают с такой структурой PDF.

--- Начало PDF ---

|                   |                   |
|-------------------|-------------------|
| Table1 Row1 Cell1 | Table1 Row1 Cell2 |
| Table1 Row2 Cell1 | Table1 Row2 Cell2 |  

|                   |                   |
|-------------------|-------------------|
| Table2 Row1 Cell1 | Table2 Row1 Cell2 |
| Table2 Row2 Cell1 | Table2 Row2 Cell2 |

--- Конец PDF ---

### Примеры
#### Вернуть все таблицы из документа
```csharp
select(tables) // PdfTable[]
```

Такой запрос на выходе вернет следующий JSON. 

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