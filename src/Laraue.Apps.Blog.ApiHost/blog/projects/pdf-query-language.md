---
title: Pdf Query Language
type: project
githubLink: https://github.com/win7user10/Laraue.PdfQL
tags: [PdfQL,Pdf]
description: The programming language designed to query PDF elements.
createdAt: 2025-11-01
updatedAt: 2025-11-06
---
## Key Features
|              |                                                                 |
|--------------|-----------------------------------------------------------------|
| Language     | C#                                                              |
| Framework    | .NET 9                                                          |
| Project type | Library                                                         |
| Status       | Concept                                                         |
| License      | AGPL-3.0                                                        |
| Nuget        | ![latest version](https://img.shields.io/nuget/v/Laraue.PdfQL)  |
| Downloads    | ![latest version](https://img.shields.io/nuget/dt/Laraue.PdfQL) |
| Github       | [Laraue.PdfQL](https://github.com/win7user10/Laraue.PdfQL)      |
| Application  | [Pdf Extractor](https://laraue.com/pdf-extractor)               |

## About Documents And Their Processing
In the modern world, billions of documents are stored in digital format. Popular formats include PDF, XML, DOC, DOCX,
and more. Each format has its own structure (sometimes similar), and viewing and processing require different software.

But what if users want to process all formats as if they were one? For example, a software engineer may need to extract
content from all documents containing a specific string. While such cases are not common, companies that collect datasets
or provide full-text search across documentation might be interested in a solution that indexes data in any format.
Structured documents can then be fed into AI systems, and the solution could offer a universal API to extract content
from any file.

## How The Document Query Language Can Be Implemented
I think of each document as an object with various attributes. For instance, each document can consist of text rows.
I propose that even MP3 and PNG formats may have a string representation. For MP3, this would require recognizing
song lyrics; for PNG, it would involve recognizing what is drawn in the image. In both cases, the object would have its
own string representation, meaning it can be indexed.

## About PDF Documents
The idea is to test the concept with PDF documents. PDF files can contain text, images, links, and even forms, making
them widely used for sharing documents that need to maintain their original appearance. Extracting data from PDFs
without additional libraries or tools is challenging due to specific encoding.  

But without that encoding, how can the PDF structure be represented? I envision it as something like this:
```
- Image (width: int, height: int, size: int)
- Table (rows: Row[], cells: Cell[], words: Word[])
- Paragraph (words: Word[])
- Image (width: int, height: int, size: int)
- Form (items: FormItem[])
```
The document is just a sequence of elements. I believe any document can be decomposed into a similar sequence.
Users can then query these elements without worrying about their source.

## The library vision
The PDF Query Language (PdfQL) is the first attempt to represent a document as a sequence of document objects.
Accessing these objects should be split into two parts:
- **DocumentObjectsExtractor:** The module that transforms PDF bytes to the sequence of document objects
- **DocumentObjectsQueryModule:** the module that allows to write queries to the object sequence

In essence, PdfQL is a language that consumes a user query with a specific syntax and a PDF file,
returning the matching object(s).

## The application vision
The demo application should allow users to try PdfQL online by submitting a query and a PDF file, returning JSON output
or error messages when the syntax is incorrect. It will also include hardcoded options, such as extracting all tables
or all images, which automatically transform into PdfQL to lower the entry threshold.

## PdfQL syntax
It will be named DocQL syntax if the experiment is a success.

When considering the syntax, the first thought was to make something SQL-like. However, SQL is best suited for working
with relational objects. Documents are rarely joined, and the main action is usually to extract content in a preferred
format, applying filters. This sounds similar to MongoDB stages.

The PdfQL language prototype for `selecting first cells from the document tables that have cell 4 with a text 'Name'`:
```pdfql
select(tables)
    ->filter((item) => item.GetCell(4).Text() == 'Name')
    ->selectMany(tableRows)
    ->map((item) => item.GetCell(1))
```

## Features
Actual list of the planned and released features
### Implemented
- **PdfQL language:** The base language rules are defined
- **Tables support:** Allowed to query tables and their elements with conditions
- **Demo application:** Users can try the current implementation via the link above
### Plans
- **Make a refactoring:** To support the concept that document objects are not only related to PDF
- **Plain text support:** Allows querying  `select(textRows)`, `select(words)`, `select(sentences)` etc.
- **Images support:** Return images matching conditions and allow applying functions like `resize(600, 400)`
- **Extend customization:** Allow using custom functions to apply to document objects

## Timeline
- **Apr 2025** Idea to implement of PDF language
- **May 2025** Studying how to build interpreters
- **Jul 2025** The base version to extract tables from PDF
- **Aug 2025** Demo application to try the language