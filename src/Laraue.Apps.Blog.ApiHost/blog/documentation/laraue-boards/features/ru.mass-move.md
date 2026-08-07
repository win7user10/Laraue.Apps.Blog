---
title: Перенос данных — перенос спейсов и досок между организациями
description: Как перенести целый спейс или отдельную доску в другую организацию в Laraue Boards. Перенос спейса или доски забирает с собой всё, что в них есть.
keywords: [перенос проекта в организацию, перенос kanban доски, миграция досок между организациями, перенос спейса telegram доска]
type: documentation
project: boards
order: 5
createdAt: 2026-05-16
updatedAt: 2026-08-07
---
**Settings → Data movement** в боковой панели позволяет перенести спейс или доску из текущей организации. Перенос забирает с собой всё, что внутри: спейс — все свои доски вместе с issues, доска — все свои issues.

![Страница Data movement с отдельными разделами Spaces и Boards](https://laraue.com/static/images/blog/docs/laraue-boards/data-movement.jpg)

## Перенос спейсов

В разделе **Spaces** отметьте те, что хотите перенести, или используйте значок обмена в отдельной строке, чтобы перенести только её. Нажмите **Move spaces**, выберите организацию назначения и подтвердите.

![Диалог Move space с выпадающим списком организации](https://laraue.com/static/images/blog/docs/laraue-boards/move-space.jpg)

## Перенос досок

В разделе **Boards** доски сгруппированы по спейсу, которому сейчас принадлежат. Отметьте нужные, или используйте значок обмена в отдельной строке. Нажмите **Move boards**, выберите организацию назначения и спейс внутри неё, и подтвердите.

![Диалог Move board с выпадающими списками организации и спейса](https://laraue.com/static/images/blog/docs/laraue-boards/move-board.jpg)

## Кто может это делать

Перенос спейсов и досок требует административного права **Move spaces and boards**. См. [Управление правами доступа](/ru/blog/documentation/laraue-boards/working-in-a-team/permissions).

## Связанные страницы

- [Организации](/ru/blog/documentation/laraue-boards/concepts/organizations)
- [Спейсы](/ru/blog/documentation/laraue-boards/concepts/spaces)
- [Эпики](/ru/blog/documentation/laraue-boards/concepts/epics)