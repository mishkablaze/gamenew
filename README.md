# Raidline

Хардкорный экстракшн-шутер от первого лица в low-poly стиле. Unity 6.6 (6000.6.7f1) + URP. Платформы: Windows и macOS.

## С чего начать

1. `docs/PLAN.md` — что делаем, рамки, бюджеты, вехи.
2. `docs/TASKS.md` — доска задач.
3. `AGENTS.md` — как работают ИИ-агенты (Claude и Codex).
4. `docs/ARCHITECTURE.md`, `docs/CONVENTIONS.md` — как устроен и как пишется код.

## Тесты


```bash
tools/test-core.sh              # тесты ядра без Unity (dotnet)
tools/test-unity.sh editmode    # тесты Unity в batchmode (проект должен быть закрыт в редакторе)
```

## Структура

См. `docs/ARCHITECTURE.md` §2.
