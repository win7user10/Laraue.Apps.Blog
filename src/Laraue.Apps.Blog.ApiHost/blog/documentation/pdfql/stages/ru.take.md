---
type: documentation
title: Take
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Операция, ограничивающая последовательность указанным количеством элементов. 

#### Синтаксис
```antlr
Take
  : 'take' '(' ConstantExpression ')'  
  ;
```

Связанные токены
_[ConstantExpression](../expression/constant)_

#### Примеры
Вернуть только три ячейки последовательности
```csharp
select(tableCells) // PdfTableCell[]
    ->take(3) // PdfTableCell[]
```