---
type: documentation
title: New Expression
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
New expression allows to create new instance. In PdfQL is used to create anonymous types. 

#### Syntax
```antlr
NewExpression 
  : 'new {' (MemberAssign)?+ '}'
  ;
```