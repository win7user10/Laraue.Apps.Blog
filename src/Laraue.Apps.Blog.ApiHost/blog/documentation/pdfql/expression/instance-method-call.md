---
type: documentation
title: Instance Method Call Expression
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Allows calling methods on an instance. In PdfQL is used to call methods on PDF objects.

#### Syntax
```antlr
InstanceMethodCallExpression
  : MemberExpression '.' '(' (parameters)?+ ')'
  ;
```