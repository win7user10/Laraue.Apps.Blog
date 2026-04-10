---
type: documentation
title: Skip
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Операция, пропускающая заданное число элементов последовательности

#### Синтаксис
```antlr
Skip
  : 'skip' '(' ConstantExpression ')'  
  ;
```

Связанные токены
_[ConstantExpression](../expression/constant)_

#### Примеры
Пропустить первую ячейку таблицы
```csharp
select(tableCells) // PdfTableCell[]
    ->skip(1) // PdfTableCell[]
```
Получить только вторую и третью строку таблицы
```csharp
select(tableCells) // PdfTableCell[]
    ->skip(1) // PdfTableCell[]
    ->take(2) // PdfTableCell[]
```