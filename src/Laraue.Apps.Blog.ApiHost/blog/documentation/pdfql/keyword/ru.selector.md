---
type: documentation
title: Selector
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Selector описывает, какая информация должна вернуться из шага [Select](../stages/select)

#### FilterStage syntax
```antlr
Selector
  : tables
  | tableRows
  | tableCells
  ;
```

