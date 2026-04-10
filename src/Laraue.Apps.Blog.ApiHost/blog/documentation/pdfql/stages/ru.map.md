---
type: documentation
title: Map
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Операция, которая преобразует последовательность объектов одного типа в последовательность объетов другого, используя
переданную функцию для трансформации.

#### Синтаксис
```antlr
MapStage
  : 'map' '(' LambdaExpression ')'  
  ;
```

Связанные токены
_[LambdaExpression](../expression/lambda)_

#### Примеры использования
Для каждой таблицы PDF получить ее текстовый контент
```csharp
select(tables) // PdfTable[]
    ->map((item) => item.Text()) // string[]
```
Для каждой строки таблицы, получить значение ее первой ячейки как 'Name' и второй ячейки, как 'Description.'
```csharp
select(tableRows) // PdfTable[]
    ->map((row) => new { Title = row.GetCell(1).Text(), Description = row.GetCell(2).Text() }) // object[]
```