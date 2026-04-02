---
type: documentation
title: Take
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Take is the operation that limits a sequence to the passed number of elements. 

#### Syntax
```antlr
Take
  : 'take' '(' ConstantExpression ')'  
  ;
```

Related tokens  
_[ConstantExpression](../expression/constant)_

Take examples
1. Returns only 3 cells from the sequence.
    ```csharp
    select(tableCells) // PdfTableCell[]
        ->take(3) // PdfTableCell[]
    ```