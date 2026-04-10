---
type: documentation
title: SelectMany
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Single is the operation that returns single object from the sequence or throws an exception if the sequence contains more 
or less elements.

#### Syntax
```antlr
First
  : 'single' '(' LambdaExpression? ')'  
  ;
```


Related tokens  
_[LambdaExpression](../expression/lambda)_

#### Single examples
Find a table cell with the text 'Alex'. Throws when not found or more than one record returns.
```csharp
select(tableCells) // PdfTableCell[]
    ->single((item) => item.Text() == 'Alex') // PdfTableCell
```