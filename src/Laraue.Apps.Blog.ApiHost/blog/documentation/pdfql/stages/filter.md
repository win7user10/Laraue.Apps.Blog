---
type: documentation
title: Filter
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Filter is the operation that returns objects from a sequence that matches the passed condition.

#### Syntax
```antlr
FilterStage
  : 'filter' '(' LambdaExpression ')'  
  ;
```

Related tokens  
_[LambdaExpression](../expression/lambda)_

#### Usage examples
For each table cell returns only those where text is equal to 'Title'.
```csharp
select(tableCells) // PdfTableCell[]
    ->filter((item) => item.Text() == 'Title') // PdfTableCell[]
```