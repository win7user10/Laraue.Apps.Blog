---
type: documentation
title: FirstOrDefault
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Операция, возвращая первый объект из последовательности или null, если последовательность пуста.

#### Syntax
```antlr
FirstOrDefaultStage
  : 'firstOrDefault' '(' LambdaExpression? ')'  
  ;
```

Связанные токены
_[LambdaExpression](../expression/lambda)_

#### Примеры
Найти ячейку таблицы, контент которой равен 'Alex'. Вернуть ```null``` если не найдено.
```csharp
select(tableCells) // PdfTableCell[]
    ->firstOrDefault((item) => item.Text() == 'Alex') // PdfTableCell?
```