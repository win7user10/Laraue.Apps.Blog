---
title: PdfQL language
icon: 📄
type: rootSectionDefinition
createdAt: 2025-08-01
updatedAt: 2025-08-01
---
A query language designed to extract structured data from PDF documents using a pipeline of composable stages.

#### Quick example
```
select(tables)
    ->filter((item) => item.GetCell(4).Text() == 'Name')
    ->selectMany(tableRows)
    ->map((item) => item.GetCell(1))
```

## ▶︎ [Stages](pdfql/stages)
Pipeline operators that transform, filter and reduce element collections. Chain them to build precise extraction queries.

## λ [Expressions](pdfql/expression)
Building blocks used inside stage predicates. Compose them to express complex matching conditions.

## 🔑 [Keywords](pdfql/keyword)
Special tokens with predefined meaning in the PdfQL grammar.

## 📥 [Output](pdfql/output)
Supported output formats for query results.