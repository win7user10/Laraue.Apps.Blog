---
type: documentation
title: Шаги
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
PdfQL - это язык, которые описывает, как полчить требуемые объекты из PDF документа. 
Каждая инструкция - это шаг, которые трансформирует данные из текущего формата в желаемый.

#### Синтаксис
```antlr
Stages
  : Stage ('->' Stage)*
  ;
```

### Пример выражения на PdfQL

```csharp
select(tables) // PdfTable[] - Получить все таблицы из документа
    ->filter((item) => item.GetCell(4).Text() == 'Name') // PdfTable[] - Вернуть только те таблицы, в которых ячейка #4 содержит текст 'Name'
    ->selectMany(tableRows) // PdfTableRow[] - Получить все строки из таблицы, и трансформировать сз двумерного в одномерный массив
    ->map((item) => item.GetCell(1).Text()) // string - Из каждой строки вернуть только текст из колонки #1.
```

## Синаксис шага PdfQL

Шаг может быть одним из значений
```antlr
Stage
  : SelectStage
  | SelectManyStage
  | FilterStage
  | MapStage
  | SingleStage
  | FirstOrDefaultStage
  | FirstStage
  ;
```