---
type: documentation
title: First
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
First is the operation that returns first object from the sequence or throws an exception if the sequence
does not contain elements.

#### Syntax
```antlr
First
  : 'first' '(' LambdaExpression? ')'  
  ;
```

Related tokens  
_[LambdaExpression](../expression/lambda)_

First examples
1. Find a table cell with the text 'Alex'. Throws when not found.
    ```csharp
    select(tableCells) // PdfTableCell[]
        ->first((item) => item.Text() == 'Alex') // PdfTableCell
    ```