---
type: documentation
title: Stages
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
PdfQL is the language that describes how to get objects from PDF document. 
Each instruction is the stage that transforms data from current input to described output.

#### Syntax
```antlr
Stages
  : Stage ('->' Stage)*
  ;
```

### PdfQL example

```csharp
select(tables) // PdfTable[] - Get all tables from a document
    ->filter((item) => item.GetCell(4).Text() == 'Name') // PdfTable[] - Returns only tables where cell #4 contains text 'Name'
    ->selectMany(tableRows) // PdfTableRow[] - Get all table rows from tables, and transaform two-dimension array to one dimension
    ->map((item) => item.GetCell(1).Text()) // string - From table rows get cell #1 text.
```


## PdfQL stage syntax

Stage can be one of the following tokens
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