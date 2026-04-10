---
type: documentation
title: SelectMany
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
SelectMany is the operation that can get from the objects collection the sequence of requested objects.

#### Syntax
```antlr
SelectManyStage
  : 'selectMany' '(' Selector ')'  
  ;
```
Related tokens  
_[Selector](../keyword/selector)_

#### Usage examples
Select table rows
```csharp
select(tables)->selectMany(tableRows) // PdfTableRow[]
```
Select table cells
```csharp
select(tables)->selectMany(tableCells) // PdfTableCell[]
```