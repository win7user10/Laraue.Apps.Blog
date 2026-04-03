---
type: documentation
title: Map
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Map is the operation that transforms sequence of objects to the sequence of other objects using map function.

#### Syntax
```antlr
MapStage
  : 'map' '(' LambdaExpression ')'  
  ;
```

Related tokens  
_[LambdaExpression](../expression/lambda)_

Map examples
1. For each table in a PDF take the text content
    ```csharp
    select(tables) // PdfTable[]
        ->map((item) => item.Text()) // string[]
    ```
2. For each table row get first cell text as 'Name' and second cell as 'Description.'
    ```csharp
    select(tableRows) // PdfTable[]
        ->map((row) => new { Title = row.GetCell(1).Text(), Description = row.GetCell(2).Text() }) // object[]
    ```