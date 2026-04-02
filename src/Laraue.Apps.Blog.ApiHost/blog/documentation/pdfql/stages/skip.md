---
type: documentation
title: Skip
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Skip is the operation that skips passed number of elements in a sequence.

#### Syntax
```antlr
Skip
  : 'skip' '(' ConstantExpression ')'  
  ;
```

Related tokens  
_[ConstantExpression](../expression/constant)_

Skip examples
1. Skip first cell of the sequence.
    ```csharp
    select(tableCells) // PdfTableCell[]
        ->skip(1) // PdfTableCell[]
    ```
2. Take only the second and the third rows.
    ```csharp
    select(tableCells) // PdfTableCell[]
        ->skip(1) // PdfTableCell[]
        ->take(2) // PdfTableCell[]
    ```