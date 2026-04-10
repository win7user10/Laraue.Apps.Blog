---
type: documentation
title: Вызов метода
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Позволяет вызывать методы объекта. В PdfQL используется для вызова методов у PDF объектов.

#### Syntax
```antlr
InstanceMethodCallExpression
  : MemberExpression '.' '(' (parameters)?+ ')'
  ;
```