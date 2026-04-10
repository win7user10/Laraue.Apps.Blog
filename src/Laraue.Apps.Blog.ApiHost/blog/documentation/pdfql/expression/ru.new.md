---
type: documentation
title: Новый экземпляр
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Позволяет создавать новые объекты. В PdfQL используется для создания анонимных типов. 

#### Syntax
```antlr
NewExpression 
  : 'new {' (MemberAssign)?+ '}'
  ;
```