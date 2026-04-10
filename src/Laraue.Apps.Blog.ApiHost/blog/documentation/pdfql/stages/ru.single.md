---
type: documentation
title: SelectMany
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Операция, возвращаяющая первый объект из последовательности или вызывающая исключение, если последовательность не содержит элементов,
или содержит более одного элемента.

#### Синтаксис
```antlr
First
  : 'single' '(' LambdaExpression? ')'  
  ;
```

Связанные токены
_[LambdaExpression](../expression/lambda)_

Примеры
Найти ячейку таблицы с текстом 'Alex'. Выбросить исключение, если нет ни одной такой ячейки, или их более, чем одна.
```csharp
select(tableCells) // PdfTableCell[]
    ->single((item) => item.Text() == 'Alex') // PdfTableCell
```