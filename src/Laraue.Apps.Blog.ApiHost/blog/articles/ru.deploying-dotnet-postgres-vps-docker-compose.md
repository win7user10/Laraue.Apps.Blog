---
title: Деплой .NET-приложения и PostgreSQL на дешёвый VPS через Docker Compose
description: Часть 6 цикла о разработке Telegram-таск-трекера в одиночку. Деплой на дешевый VPS через Docker Compose — self-hosted PostgreSQL, настроенный под 1 ГБ RAM, GitHub CI-пайплайн, работающий на push в main и описание докерфайлов.
type: article
createdAt: 2026-06-22 12:00
updatedAt: 2026-07-07 09:00
projects: [boards]
tags: [docker, docker-compose, postgres, vps, self-hosting, devops, dotnet, devlog]
previousLink: clean-dotnet-telegram-bot-architecture
nextLink: deploy-nuxt-telegram-mini-app-https-nginx
---

> **Architecture First: как в одиночку с ИИ сделать альтернативу Jira** — Часть 6.
> [Предыдущая статья](clean-dotnet-telegram-bot-architecture) закончилась первой версией бэкенда, работающей только на локальной машине. В этой разворачиваем её на реальном сервере.

Цель статьи — развернуть бота на самой дешёвой инфраструктуре, которая только может справиться с задачей: небольшой VPS, Docker Compose, self-hosted PostgreSQL с настроенными лимитами памяти и GitHub CI-пайплайн, выгружающий артефакты приложения на каждый push в `main`.

## Сервер: один VPS на всю инфраструктуру

Мы не строим огромную корпоративную систему и не хотим тратить большие суммы, пока проект не окупается, поэтому Kubernetes, managed-контейнеров и облачной базы не будет — всё будет запущено на одном небольшом VPS.

Отправной точкой стал уже существующий сервер: этот блог работает на VPS с 1 ГБ RAM, и провайдер позволял увеличить память до 3 ГБ в несколько кликов. Расчёт такой: примерно 1 ГБ на приложения, 1 ГБ на PostgreSQL, остальное — операционной системе, блогу и демо-приложениям. Один сервер за несколько долларов в месяц способен хостить все эту пачку.

### Self-hosted PostgreSQL вместо managed-базы

Managed-база имеет ряд преимуществ: автоматические бэкапы, failover и point-in-time recovery. Это реальные плюсы, но платить за них имеет смысл, когда есть что защищать. Self-hosted PostgreSQL на том же VPS фактически бесплатен — сервер уже оплачен, а нагрузки, которую он не смог бы переварить, пока нет, а бэкапы сервера позволяют иметь некоторую гарантию сохранности данных. Когда цена потери данных вырастет, переезд на managed-базу может стать разумным шагом.

Та же логика позже применится и к файлам: медиа будут храниться в файловой системе VPS, а не в объектном хранилище вроде S3, чтобы не тратиться на инфраструктуру.

## GitHub CI пайплайн

Выгрузка артефактов на сервер делается при каждом пуше в ветку `main` с помощью функциональности GitHub Actions. Workflow лежит в `.github/workflows/` и называется `dotnet.yml`:

```yaml
name: .NET
on:
  push:
    branches: [ "main" ]
  pull_request:
    branches: [ "main" ]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 10.x.x
    - name: Restore dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build --no-restore -c Release
    - name: Upload Telegram Host
      if: github.ref == 'refs/heads/main'
      uses: marcodallasanta/ssh-scp-deploy@v1.2.0
      with:
       host: ${{ secrets.SSH_HOST }}
       port: ${{ secrets.SSH_PORT }}
       user: ${{ secrets.SSH_USER }}
       password: ${{ secrets.SSH_PASSWORD }}
       local: src/Laraue.Apps.Boards.TelegramHost/bin/Release/net10.0/*
       remote: "/home/laraue/boards-telegram-host"
       post_upload: echo "Uploaded successfully"
```

Пайплайн начинает работать сразу после того, как будет запушен в репозиторий. Его задача — по пушу в `main` собрать проект, используя .NET 10, и выгрузить полученный артефакт в `/home/laraue/boards-telegram-host` по SSH. Детали подключения — GitHub secrets, задаются в Settings → Secrets and variables → Actions самого GitHub и в кодовой базе не появляются.

## Dockerfile

Артефакты бота упаковываются в Docker-контейнер с помощью небольшого Dockerfile с названием `BoardsTelegramHostDockerfile`, хранящегося на сервере:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY boards-telegram-host app
WORKDIR app
ENTRYPOINT ["dotnet", "Laraue.Apps.Boards.TelegramHost.dll"]
```

В контейнер копируются готовые файлы, полученные на этапе сборки в CI. Контейнеру нужен только runtime .NET для запуска хоста, а не полный .NET SDK, поэтому его размер остаётся маленьким.

### Почему для healthcheck нужно ставить curl

Единственная неочевидная строка в Dockerfile — `apt-get install curl`. Runtime-образы .NET минимальны: начиная с .NET 8 в них нет ни `curl`, ни `wget`. Приложению это не мешает, но ломает healthcheck контейнера, которому нужно сделать HTTP-запрос к эндпоинту `/_health` запущенного приложению.

## Docker Compose: бот и база в одном файле

Compose связывает два контейнера — хост бота и PostgreSQL — в один деплой:

```yaml
version: '3.4'
networks:
  dockerapi-dev:
    driver: bridge
services:
  boardstelegramhost:
    build:
      context: .
      dockerfile: "BoardsTelegramHostDockerfile"
    expose:
      - "5006"
    ports:
      - "8086:5006"
    restart: always
    environment:
      ASPNETCORE_ENVIRONMENT: "Production"
      Kestrel__EndPoints__Http__Url: "http://+:5006"
      Telegram__Token: "TokenHere"
      ConnectionStrings__Postgre: "User ID=PostgresUser;Password=PostgresPass;Host=postgres;Port=5432;Database=laraue_messages_board;Command Timeout=0;"
      Logging__LogLevel__Default: "Warning"
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - dockerapi-dev
    deploy:
      resources:
        limits:
          memory: 256M
    healthcheck:
      test: ["CMD-SHELL", "curl -fsS http://localhost:5006/_health || exit 1"]
      interval: 5s
      timeout: 3s
      retries: 10
      start_period: 10s

  postgres:
    image: postgres:18-alpine
    container_name: postgres_db
    restart: always
    environment:
      POSTGRES_USER: PostgresUser
      POSTGRES_PASSWORD: PostgresPass
    volumes:
      - /home/laraue/postgres_data:/var/lib/postgresql/data
      - ./postgres.conf:/etc/postgresql/postgresql.conf
      - ./pg_hba.conf:/etc/postgresql/pg_hba.conf
    networks:
      - dockerapi-dev
    expose:
      - "5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U PostgresUser -d laraue_messages_board"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 10s
    deploy:
      resources:
        limits:
          memory: 1024M
```

Несколько комментариев по принятым решениям.

**Лимиты устанавливаются только по памяти.** У каждого сервиса есть ограничение по памяти, но нет лимита по CPU. VPS одноядерный, и вручную заданные ограничения в этом случае скорее мешают: сервисы стартуют намного медленнее, чем могли бы, база работает медленнее. Лимиты по памяти при этом нужны и являются хорошей подстраховкой: они не дают одному контейнеру занять всю память и спровоцировать `OutOfMemoryException` у соседнего сервиса. Поэтому память ограничена явно, а CPU управляется стандартным планировщиком ОС.

**Healthcheck использует эндпоинт из прошлой статьи.** Эндпоинт `/_health`, добавленный в хост, опрашивается через `curl`. Контейнер перезапускается, если тот перестает отвечать. У Postgres проверка работоспособности сделана через `pg_isready`. Бот ожидает запуска базы через `depends_on`, чтобы не начать запуск до момента работоспособности БД.

**Postgres хранит свои данные через bind mount.** Директория VPS `/home/laraue/postgres_data` маппится в директорию `/var/lib/postgresql/data` контейнера, куда Postgres сохраняет данные. Данные физически лежат на диске VPS и видны в его файловой системе.

**Секреты в файле — не настоящие.** Реальные значения задаются на сервере в этом же файле.

### Настройка Postgres под 1 ГБ памяти

Self-hosted база означает в первую очередь то, что настраивать ее придется самостоятельно. Дефолтная конфигурация позволяет Postgres запускаться на серверах с минимальным количеством RAM, но работа на более мощных серверах становится неоптимальной. Существуют специальные сервисы, позволяющие подобрать оптимальные настройки Postgres под конкретную конфигурацию сервера. Мы остановились на таком варианте `postgresql.conf`:

```ini
shared_buffers = 256MB
effective_cache_size = 768MB
work_mem = 16MB
maintenance_work_mem = 64MB
max_connections = 75
wal_buffers = 16MB
min_wal_size = 1GB
max_wal_size = 2GB
checkpoint_completion_target = 0.9
random_page_cost = 1.1
effective_io_concurrency = 200
```

Два основных параметра — `shared_buffers` (256 МБ, память под кеш страниц данных) и `effective_cache_size` (768 МБ, подсказка планировщику о доступном дисковом кеше). `random_page_cost = 1.1` сообщает планировщику, что случайные чтения дёшевы, — это рекомендованное значение для серверов с SSD. Это стандартная настройка Postgres под реально доступную RAM — ровно то, что managed-база делает за вас. Файл с конфигурацией находится рядом с файлом `docker-compose`, и пробрасывается в контейнер так: `./postgres.conf:/etc/postgresql/postgresql.conf`.

### Ограничение доступа к базе через pg_hba.conf

Хорошей практикой является ограничение публичного доступа к БД. Доступ к Postgres ограничивается через `pg_hba.conf`: подключаться разрешено только одному внешнему IP, всё остальное отклоняется:

```
# TYPE  DATABASE  USER  ADDRESS         METHOD
local   all       all                   trust
host    all       all   127.0.0.1/32    md5
host    all       all   ServerIp/32     md5
host    all       all   0.0.0.0/0       reject
```

Мы доверяем локальным подключениям и одному IP, все остальные подключения отклоняются. Postgres останавливается на первом подходящем правиле, финальная строка reject — перестраховка для ясности.

`ServerIp` здесь — адрес self-hosted-VPN, поднятого на том же сервере. Мы подключаемся к нему с локальной машины (Amnezia с AmneziaWG), и пока VPN активен, локальный компьютер выглядит для Postgres доверенным IP — базу можно открыть напрямую из инструмента вроде DataGrip.

## Подключение к Telegram: long polling

Бот взаимодействует с Telegram через long polling — сам запрашивает апдейты, а не принимает их на публичный вебхук. Это упрощает деплой: настройка публичного HTTPS адреса для сервера пока не потребуется.

## Структура файлов на сервере

Всё, что относится к нашим проектам, живёт на VPS в директории `/home/laraue/`:

- здесь появляются папки с залитыми артефактами — например, `boards-telegram-host`, куда CI кладёт собранного бота (рядом появится `boards-webapi-host` для web API из следующих статей);
- Dockerfile'ы для каждого приложения;
- конфиг-файлы — `postgres.conf`, `pg_hba.conf`;
- `docker-compose.yml`, описывающий все сервисы;
- директории данных для bind mount — пока это только `postgres_data`.

Обновление сервисов после заливки артефактов выполняется вручную. Мы подключаемся к VPS по SSH и выполняем две команды:

```bash
docker-compose build
docker-compose up -d
```

`build` собирает обновленные сервисы, `up -d` пересоздаёт изменившиеся контейнеры. Для ситуации, когда деплоем всегда занимается один человек — это довольно просто и предсказуемо.

## Итоги

Бот задеплоен на дешёвом VPS: он работает в Docker, рядом развернута self-hosted PostgreSQL, сконфигурированная под 1 ГБ RAM и защищенная от публичного доступа. Артефакты выкладываются через GitHub Actions по push в `main`. Бот запущен и работает с Telegram через long polling и вот как это выглядит:

![Реакция бота на новое сообщение](https://laraue.com/static/images/blog/articles/laraue-boards/message-bot-board-example.jpg)

## Что дальше

Бот сохраняет задачи в базу, но управление ими пока не доступно. Следующая статья будет о фронтенде: как сделать так, чтобы он открылся внутри Telegram как Mini App. Расскажем о настройке HTTPS в nginx с бесплатным сертификатом от Let's Encrypt и задеплоим фронтенд.