---
type: documentation
title: Filter
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Фильтр - операция, которая возвращает из последовательности элементы, удовлетворяющие условию.

#### Синтаксис
```antlr
FilterStage
  : 'filter' '(' LambdaExpression ')'  
  ;
```

Связанные токены  
_[LambdaExpression](../expression/lambda)_

#### Примеры использования
Из всех ячеек таблицы вернуть те, где текст равен 'Title'.
 ```csharp
select(tableCells) // PdfTableCell[]
    ->filter((item) => item.Text() == 'Title') // PdfTableCell[]
```