---
type: documentation
title: Лябмда
project: PdfQL
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
Позволяет определять функцию. В PdfQL для указания маппинга или фильтраций.

#### Syntax
```antlr
LambdaExpression 
  : '(' (parameterNames)? ')' '=>' Expression
  ;
```