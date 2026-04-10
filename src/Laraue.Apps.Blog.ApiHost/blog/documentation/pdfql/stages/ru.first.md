---
type: documentation
title: First
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Операция, возвращаяющая первый объект из последовательности или вызывающая исключение, если последовательность пуста.

#### Синтаксис
```antlr
First
  : 'first' '(' LambdaExpression? ')'  
  ;
```

Связанные токены
_[LambdaExpression](../expression/lambda)_

#### Примеры
Найти ячейку таблицы с контентом 'Alex'. Выбросить исключение, если такая не найдена.
```csharp
select(tableCells) // PdfTableCell[]
    ->first((item) => item.Text() == 'Alex') // PdfTableCell
```