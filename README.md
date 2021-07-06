# Фрейморез

## Зачем

Нарезает фреймов для стримов.


[![Github All Releases](https://img.shields.io/github/downloads/kasthack-labs/kasthack.framecutter/total.svg)](https://github.com/kasthack-labs/kasthack.framecutter/releases/latest)
[![GitHub release](https://img.shields.io/github/release/kasthack-labs/kasthack.framecutter.svg)](https://github.com/kasthack-labs/kasthack.framecutter/releases/latest)
[![license](https://img.shields.io/github/license/kasthack-labs/kasthack.framecutter.svg)](LICENSE)
[![.NET Status](https://github.com/kasthack-labs/kasthack.framecutter/workflows/.NET/badge.svg)](https://github.com/kasthack-labs/kasthack.framecutter/actions?query=workflow%3A.NET)
[![CodeQL](https://github.com/kasthack-labs/kasthack.framecutter/workflows/CodeQL/badge.svg)](https://github.com/kasthack-labs/kasthack.framecutter/actions?query=workflow%3ACodeQL)
[![Patreon pledges](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Fshieldsio-patreon.vercel.app%2Fapi%3Fusername%3Dkasthack%26type%3Dpledges&style=flat)](https://patreon.com/kasthack)
[![Patreon patrons](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Fshieldsio-patreon.vercel.app%2Fapi%3Fusername%3Dkasthack%26type%3Dpatrons&style=flat)](https://patreon.com/kasthack)

## Использование

### Нормальный пользователь

[@framecutter_bot](https://t.me/framecutter_bot)

### Свой деплой

В папке с исходниками лежит `docker-compose`. Для запуска бота нужно только прописать токен в файле `.env` оттуда же. Если нужно поменять фон / параметры для вставки изображений, поправьте appsettings.json и пересоберите контейнер(можно подмонтировать bind-директорию)

Для запуска без докера на linux требуется `libgdiplus`(пакет есть в стандартной репе debian). На windows нужен .net, если не использовать self-contained deployment.