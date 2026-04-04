---
type: documentation
title: Select
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Select это операция, которая может получит из объекта набор запрашиваемых объектов.

#### Синтаксис
```antlr
SelectStage
  : 'select' '(' Selector ')'  
  ;
```

Связанные токены
_[Selector](../keyword/selector)_

#### Примеры использования

##### Выборка таблиц
 ```csharp
 select(tables) // PdfTable[]
 ```

##### Выбора строк таблиц
 ```csharp
 select(tableRows) // PdfTableRow[]
 ```

##### Выборка ячеек таблиц
 ```csharp
 select(tableCells) // PdfTableCell[]
 ```