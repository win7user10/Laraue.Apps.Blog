---
type: documentation
title: FirstOrDefault
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
FirstOrDefault is the operation that returns first object from the sequence or null if the sequence is empty.

#### Syntax
```antlr
FirstOrDefaultStage
  : 'firstOrDefault' '(' LambdaExpression? ')'  
  ;
```

Related tokens  
_[LambdaExpression](../expression/lambda)_

FirstOrDefault examples
1. Find a table cell with the text 'Alex'. Returns ```null``` if not found.
    ```csharp
    select(tableCells) // PdfTableCell[]
        ->firstOrDefault((item) => item.Text() == 'Alex') // PdfTableCell?
    ```