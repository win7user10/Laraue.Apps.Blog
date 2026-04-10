---
type: documentation
title: SelectMany
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Операция, превращающая коллекцию коллекций объектов в коллекцию объектов.

#### Синтаксис
```antlr
SelectManyStage
  : 'selectMany' '(' Selector ')'  
  ;
```
Связанные токены 
_[Selector](../keyword/selector)_

#### Примеры
Выбрать строки таблиц, вернуть их последовательно в одном массиве
```csharp
select(tables)->selectMany(tableRows) // PdfTableRow[]
```
Выбрать ячейки таблиц, вернуть их последовательно в одном массиве
```csharp
select(tables)->selectMany(tableCells) // PdfTableCell[]
```