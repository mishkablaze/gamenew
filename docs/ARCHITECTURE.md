# Архитектура Raidline

> Как устроен код и почему. Если реализация расходится с документом, то либо исправляется код, либо документ обновляется вместе с записью в `docs/DECISIONS.md`. Молча расходиться нельзя.

---

## 1. Принципы

1. **Ядро без движка.** Правила игры (инвентарь, урон, пробитие, экономика, сохранения) живут в сборке `Raidline.Core`, которая не ссылается на `UnityEngine`. Её можно тестировать через `dotnet test` без редактора, а позже запускать на сервере.
2. **Симуляция отдельно от отображения.** Геймплей никогда не зависит от эффектов, звука, анимаций и UI. Отображение подписывается на события симуляции.
3. **Один код для игрока и бота.** Персонаж — это набор компонентов, которым управляет источник ввода: игрок или мозг бота.
4. **Все изменения состояния — через команды.** UI и ввод ничего не меняют напрямую: они отправляют команду, авторитет (сейчас локальный, потом сервер) её проверяет и применяет.
5. **Фиксированный тик.** Геймплей считается с частотой 60 Гц, камера и рендер обновляются каждый кадр с интерполяцией.
6. **Ноль аллокаций в горячих путях.** Пулы, структуры, заранее выделенные буферы.
7. **Явные зависимости.** Без DI-фреймворков и без статических синглтонов с изменяемым состоянием. Сервисы создаёт composition root и передаёт явно.

---

## 2. Структура репозитория

Корень репозитория — это и корень Unity-проекта.

```
Raidline/
├─ AGENTS.md                 протокол для агентов (Codex читает автоматически)
├─ CLAUDE.md                 то же для Claude (+ импорт AGENTS.md)
├─ README.md
├─ docs/                     план, архитектура, задачи, решения, журнал
├─ tools/
│  ├─ test-core.sh           быстрые тесты Core через dotnet (без Unity)
│  ├─ test-unity.sh          EditMode/PlayMode-тесты Unity в batchmode
│  ├─ build.sh               сборки через BuildScript (M9)
│  └─ CoreTests/             dotnet-проект, компилирующий Core + тесты Core
├─ Assets/
│  ├─ _Project/
│  │  ├─ Code/
│  │  │  ├─ Core/            Raidline.Core            — правила, без UnityEngine
│  │  │  ├─ Infrastructure/  Raidline.Infrastructure  — тики, пулы, ввод, файлы, логи, настройки
│  │  │  ├─ Data/            Raidline.Data            — ScriptableObject-определения и базы
│  │  │  ├─ Gameplay/        Raidline.Gameplay        — персонаж, оружие, баллистика, урон, лут, рейд
│  │  │  ├─ AI/              Raidline.AI              — боты
│  │  │  ├─ Presentation/    Raidline.Presentation    — FX, звук, анимации от 1-го лица, эффекты камеры
│  │  │  ├─ UI/              Raidline.UI              — экраны и HUD на UI Toolkit
│  │  │  ├─ App/             Raidline.App             — composition root, переходы между сценами
│  │  │  └─ Editor/          Raidline.Editor          — инструменты редактора
│  │  ├─ Tests/
│  │  │  ├─ Core/            Raidline.Core.Tests      — EditMode, тоже собирается dotnet
│  │  │  ├─ EditMode/        Raidline.Tests.EditMode  — тесты, которым нужен UnityEngine
│  │  │  └─ PlayMode/        Raidline.Tests.PlayMode
│  │  ├─ Content/
│  │  │  ├─ Definitions/     SO-ассеты: Items/, Weapons/, Ammo/, Armor/, Meds/, Loot/, Bots/, Traders/, Quests/
│  │  │  ├─ Characters/  Weapons/  Props/  Environment/   модели и префабы
│  │  │  ├─ Materials/  Textures/                          палитра и материалы
│  │  │  ├─ Audio/  VFX/
│  │  │  └─ UI/              UXML, USS, иконки
│  │  ├─ Scenes/             Boot, MainMenu, Raid_Map01_*, Sandbox, Benchmark
│  │  └─ Settings/           URP-ассеты, Volume-профили, InputActions
│  └─ ThirdParty/            купленные и скачанные ассеты: не правим, только используем
├─ Packages/
└─ ProjectSettings/
```

---

## 3. Сборки (asmdef) и зависимости

```
Core  ◄── Infrastructure ◄──┐
  ▲            ▲            │
  └── Data ◄───┴── Gameplay ◄── AI
                      ▲  ▲
                      │  └──── Presentation
                      └─────── UI
App → ссылается на всё (composition root)
Editor → ссылается на всё, только Editor-платформа
```

| Сборка | Ссылается на | Особенности |
|---|---|---|
| `Raidline.Core` | — | `noEngineReferences: true`; C# 9; без LINQ в горячих путях |
| `Raidline.Infrastructure` | Core, Unity.InputSystem | TickRunner, UpdateRunner, пулы, файловый IO, логи |
| `Raidline.Data` | Core | ScriptableObject-реализации интерфейсов Core |
| `Raidline.Gameplay` | Core, Infrastructure, Data | авторитетная симуляция |
| `Raidline.AI` | Core, Infrastructure, Data, Gameplay | мозги ботов работают через `ICharacterInput` |
| `Raidline.Presentation` | Core, Infrastructure, Data, Gameplay | в серверной сборке не нужна |
| `Raidline.UI` | Core, Infrastructure, Data, Gameplay | читает состояние, отправляет команды |
| `Raidline.App` | все выше | Boot, переходы, создание контекстов |
| `Raidline.Editor` | все | только `Editor` |

**Правило:** нижний слой никогда не ссылается на верхний. Если Gameplay должен «сообщить» Presentation или UI, он публикует событие (§6).

---

## 4. Сцены, composition root, контексты

### Сцены

| Сцена | Назначение |
|---|---|
| `Boot` | Единственная сцена в Build Settings под индексом 0. `AppRoot` создаёт глобальные сервисы и живёт всю сессию |
| `MainMenu` | Меню, схрон, торговцы, задания, подготовка к рейду |
| `Raid_Map01_Geometry` | Геометрия, освещение, окклюзия, NavMesh |
| `Raid_Map01_Gameplay` | Спавны, лут-точки, выходы, зоны ботов; `RaidRoot` |
| `Sandbox` | Песочница для механик (M1+) |
| `Benchmark` | Фиксированный маршрут камеры с выгрузкой CSV (M1+) |

Сцены рейда грузятся аддитивно и выгружаются целиком по окончании рейда.

### Контексты и время жизни сервисов

- **`AppContext`** живёт всю сессию и создаётся `AppRoot`: `TickRunner`, `UpdateRunner`, `InputService`, `SaveService`, `SettingsService`, `ItemDatabase`, `AudioService`, `SceneFlow`, `Log`.
- **`RaidContext`** живёт один рейд и создаётся `RaidRoot`: `EntityRegistry`, `RaidEventBus`, `BallisticsSystem`, `NoiseBus`, `BotManager`, `LootSpawner`, `ExtractionSystem`, `RaidSession`, `Rng` (seed рейда).

MonoBehaviour-компоненты получают зависимости методом `Init(RaidContext ctx)` (или `Init(AppContext)`), который вызывает root или фабрика. `FindObjectOfType`, `GameObject.Find` и статические `Instance` для геймплейного состояния **запрещены**.

---

## 5. Время: тик и обновления

### Тик симуляции

- `Raidline.Core.Time.TickClock` — чистый аккумулятор: `dt = 1/60`, максимум 4 тика за кадр (защита от «спирали смерти»), отдаёт `alpha` для интерполяции. Покрыт тестами.
- `TickRunner` (Infrastructure) — один MonoBehaviour, который каждый кадр крутит `TickClock` и вызывает `ITickable.Tick(in TickContext ctx)` **по фазам в фиксированном порядке**:

```
1. Input        — снимок ввода в CharacterInputFrame (игрок и боты)
2. Characters   — движение, выносливость, состояние позы
3. Weapons      — стрельба, перезарядка, отдача (выпуск снарядов)
4. Physics      — Physics.Simulate(dt)  (Physics.simulationMode = Script)
5. Ballistics   — перемещение снарядов, raycast-сегменты, попадания
6. Damage       — применение урона, эффекты здоровья
7. AI           — мозги ботов (по бюджету и LOD, §12)
8. Raid         — таймер, эвакуация, спавны
```

- Ввод мыши и поворот камеры применяются **каждый кадр** (а не каждый тик), чтобы не было задержки ввода. В симуляцию передаётся итоговый угол на момент тика.
- Визуальные объекты интерполируют позицию между двумя последними тиками по `alpha`.

### Обычные обновления

`UpdateRunner` — один MonoBehaviour на `Update`/`LateUpdate`, который вызывает зарегистрированные `IFrameUpdatable` и `ILateFrameUpdatable`. Компоненты регистрируются в `OnEnable` и снимаются в `OnDisable`. Свои `Update()` в сотнях компонентов не пишем.

**Геймплейная логика в `Update` / `LateUpdate` / `FixedUpdate` запрещена**: только в `ITickable`. В `Update` допустимы отображение, UI и камера.

---

## 6. Сущности, события, команды

### Сущности

`EntityId` — `readonly struct` с `int`. Его выдаёт `EntityRegistry` персонажам, предметам в мире, контейнерам, дверям и выходам. В M10 он однозначно отображается на сетевой ID. Снаряды сущностями не являются (§9).

### События

- `EventChannel<T> where T : struct` — типизированный канал без упаковки (boxing) и без аллокаций при публикации. Подписка — делегатом.
- `RaidEventBus` держит каналы рейда (`ShotFired`, `ProjectileHit`, `DamageApplied`, `CharacterDied`, `ItemPickedUp`, `ExtractionProgress` …) и очищается в конце рейда.
- UnityEvent в коде геймплея не используем.

### Команды и авторитет

```
Ввод/UI ──► Command (struct) ──► IAuthority ──► валидация в Core ──► применение ──► событие ──► UI/Presentation
```

- Персонаж получает `CharacterInputFrame` (движение, взгляд, кнопки) от `ICharacterInput`: `PlayerInputSource` или `BotBrain`.
- Инвентарь меняется только через `InventoryService.Execute(in InventoryCommand cmd)` и возвращает `InventoryResult` (успех или код ошибки).
- `IAuthority` сейчас реализован как `LocalAuthority` (применяет сразу). В M10 появится `ServerAuthority` / `ClientProxy`.

---

## 7. Данные и ID

- В Core живут интерфейсы определений: `IItemDefinition`, `IAmmoDefinition`, `IArmorDefinition`, `IWeaponDefinition`, `IMedicalDefinition` …
- В Data их реализуют ScriptableObject-классы: `ItemDefinitionAsset : ScriptableObject, IItemDefinition` и т.д.
- **Стабильный строковый ID** у каждого определения: `category.group.name` в нижнем регистре, например `ammo.556.fmj`, `armor.vest.class3_soft`, `med.bandage`. ID не переименовываются никогда, потому что на них ссылаются сохранения. Переименовать можно отображаемое имя.
- `ItemDatabaseAsset` — один SO со списком всех определений. В рантайме это `ItemDatabase` со словарём `id → definition` и компактным `int`-индексом для горячих путей.
- Редакторный валидатор (T-405) проверяет дубликаты ID, пустые поля, отсутствующие иконки и размеры сетки.
- Все настраиваемые числа (HP частей тела, шансы кровотечения, параметры отдачи, бюджеты ИИ) хранятся в SO-конфигах, а не в константах кода.

---

## 8. Персонаж

Персонаж одинаков для игрока и бота:

```
Character (root, EntityId)
├─ CharacterMotor        CharacterController, движение по тику, поза, наклон, высота приседа, инерция
├─ CharacterStamina      обёртка над Core StaminaState
├─ CharacterHealth       обёртка над Core HealthState, принимает DamageInfo
├─ CharacterEquipment    обёртка над Core Equipment (инвентарь, вес)
├─ WeaponController      активное оружие, стрельба, перезарядка, отдача
├─ HitboxRig             коллайдеры частей тела на костях → BodyPart
├─ ICharacterInput       PlayerInputSource | BotBrain
└─ (Presentation) CharacterView: анимации, звуки, FP-руки для игрока
```

- Вес снаряжения влияет на скорость, инерцию и расход выносливости. Кривые лежат в SO `MovementConfig`.
- Высота приседа плавная (колесо мыши или удержание), наклоны — с проверкой коллизии головы.

---

## 9. Оружие и баллистика

### Поток выстрела

```
CharacterInputFrame.Fire
  → WeaponController (тик Weapons): режим огня, RPM, патрон в патроннике?
  → Core: Magazine.Pop() → тип патрона
  → BallisticsSystem.Spawn(origin, dir · muzzleVelocity, ammoIndex, shooterId)
  → RaidEventBus.ShotFired → Presentation (вспышка, звук), NoiseBus (ИИ слышит)
  → BallisticsSystem (тик Ballistics): интеграция + сегментные raycast
  → попадание: HitboxRef? → броня → Core.PenetrationModel → DamageInfo → CharacterHealth
             : поверхность → SurfaceType → пробитие/рикошет/остановка
  → RaidEventBus.ProjectileHit → Presentation (декаль, искры, звук)
```

### BallisticsSystem

- Снаряды хранятся массивом структур (`Projectile[]` с пулом индексов, а не GameObject): позиция, скорость, индекс патрона, стрелок, остаток пробития, пройденная дистанция и время жизни.
- Интеграция за тик: `v += g·dt; v -= k·|v|·v·dt` (упрощённое сопротивление, `k` из типа патрона); сегмент `p → p + v·dt`.
- Raycast: при ≥ 32 активных снарядах батчем через `RaycastCommand` + Jobs, иначе прямым `Physics.Raycast`. Слои: окружение, хитбоксы, без триггеров.
- Хитбоксы находятся по `Collider.GetInstanceID()` в словаре, который заполняется при включении `HitboxRig`. `GetComponent` в горячем пути не вызываем.
- Поверхности: `PhysicsMaterial → SurfaceType` (SO-таблица толщины, шанса рикошета, звука и декали).
- Лимиты: время жизни 5 с или дистанция > 1500 м — снаряд удаляется.
- Трассеры и эффекты — только в Presentation, из пулов.

### Оружие

- Оружие — это предмет (§11) со слотами: магазин, патронник, модули.
- Магазин хранит упорядоченные серии патронов (`[(ammoIndex, count), …]`), а `Pop()` берёт последний заряженный патрон. Это даёт механику «смешанного магазина».
- Отдача процедурная: вертикальная и горизонтальная составляющие, разделение на импульс камеры и импульс оружия, возврат. Параметры лежат в `WeaponDefinition` и модулях, эргономика влияет на скорость прицеливания и раскачку.

---

## 10. Здоровье, броня, урон (Core)

- `BodyPart`: `Head, Thorax, Stomach, LeftArm, RightArm, LeftLeg, RightLeg`.
- `HealthState`: HP каждой части (значения по умолчанию в `HealthConfig`: голова 40, грудь 90, живот 75, руки 60, ноги 70), активные эффекты, энергия и вода.
- **Смерть** — если HP головы или груди доходит до 0. Урон по «выбитой» конечности (HP = 0) распределяется по остальным частям с множителем из конфига.
- **Эффекты:** лёгкое и сильное кровотечение, перелом, боль (и обезболивание), обезвоживание и истощение. Шансы получения — из `DamageInfo` и конфига. Эффекты тикают в фазе Damage.
- **Броня:** класс 1–6, прочность, зоны покрытия (набор `BodyPart`), материал (влияет на износ). Шлем покрывает голову.
- `PenetrationModel.Resolve(ammo, armor, rng)` — чистая функция: шанс пробития зависит от разницы пробития патрона и класса брони, а также от остатка прочности. Возвращает пробил/не пробил, износ брони и итоговый урон (тупой урон при непробитии). Формула и константы своего дизайна, задаются в `BallisticsConfig` и покрыты табличными тестами.
- Медицина: у предмета есть ресурс, время применения и список действий (лечит HP, снимает эффект, обезболивает).

---

## 11. Инвентарь (Core)

```
ItemInstance
  ItemUid Uid                     (Guid, стабилен между сохранениями)
  IItemDefinition Def
  int Stack; float Durability
  GridContainer[] Grids           (у разгрузки может быть несколько маленьких сеток)
  SlotSet Slots                   (модули оружия, патронник, магазин; у снаряжения — свои)
  MagazineContents? Ammo          (для магазинов)

GridContainer(width, height)
  int[] cells                     (индекс размещения или -1)
  List<Placement> placements      (item, x, y, rotated)

Equipment
  слоты: Headwear, Eyewear, FaceCover, BodyArmor, TacticalRig, Backpack,
         PrimaryWeapon, SecondaryWeapon, Holster, Melee, Pockets (4×1×1), SecureContainer

ItemLocation = Grid(containerUid, gridIndex, x, y, rotated) | Slot(ownerUid, slotId) | World(entityId)
```

- **Операции** (`InventoryCommand`): Move, Rotate, Split, Merge, LoadAmmo, UnloadAmmo, Attach и Detach модуля, Use, Drop, Discard, QuickMove (автопоиск места).
- Валидация: размер, коллизии, допустимые категории слота, запрет вложений по правилам (`NestingRules`: например, рюкзак в рюкзак нельзя), ограничения защищённого контейнера.
- Автопоиск места — first-fit сканирование: сначала без поворота, потом с поворотом.
- Вес считается рекурсивно и кешируется, при изменении кеш инвалидируется вверх по цепочке родителей.
- Каждая операция атомарна: либо применена целиком, либо состояние не изменилось. Это проверяется тестами.

---

## 12. ИИ

- **`BotManager`** — общий бюджет времени ИИ на кадр (по умолчанию 1.5 мс на минимальной конфигурации). Боты обслуживаются по кругу.
- **LOD обновлений** по дистанции до игрока:

| Ярус | Дистанция | Мозг | Сенсоры | Анимация |
|---|---|---|---|---|
| Near | ≤ 50 м | 20 Гц | полные | полная |
| Mid | 50–150 м | 5 Гц | зрение реже | упрощённая |
| Far | > 150 м | 1 Гц | только слух | Animator culling |

- **Восприятие:** зрение (конус, дистанция, освещённость и поза цели; лучи видимости батчем, не больше N за кадр) и слух через `NoiseBus` (выстрелы, шаги, двери; радиус зависит от типа и поверхности).
- **Мозг:** иерархическая FSM с утилитарным выбором внутри состояний: `Idle/Patrol → Investigate → Engage → TakeCover → Reposition → Heal → Search → Flee`.
- **Навигация:** пакет AI Navigation (NavMeshSurface). Точки укрытий запекаются редакторным инструментом и выбираются по оценке (укрытие от угрозы, дистанция, наличие линии огня).
- **Бой:** время реакции, точность растёт со временем на цели и падает от дистанции, подавления и ранений. Бот стреляет через тот же `WeaponController` и теми же снарядами, что и игрок.
- **Отладка:** гизмо восприятия, текущее состояние над головой, лог решений (только в dev-сборке).

---

## 13. UI (UI Toolkit)

- Весь UI сделан на UI Toolkit: UXML (разметка), USS (стили) и C#-контроллер. Текстовые файлы удобно писать агентам и легко ревьюить.
- Экран = `ScreenController` (жизненный цикл, навигация) + презентеры виджетов, которые подписываются на события модели. UI **никогда** не меняет модель напрямую, только отправляет команды.
- Drag & drop в инвентаре реализован собственным `PointerManipulator`: призрак предмета, подсветка валидности, R — поворот при перетаскивании.
- Основное управление — мышь и клавиатура. Геймпад в срез не входит, но экраны строим на стандартных событиях навигации UI Toolkit, чтобы его можно было добавить без переписывания.
- Для большого схрона виртуализируем строки и обновляем только изменённые ячейки.
- Иконки предметов генерируются редакторным инструментом (рендер префаба в PNG по размеру сетки) и собираются в атлас.
- Читаемость на 1280×720 (минимальное разрешение) и корректное масштабирование до 4K — обязательная проверка каждого экрана.

---

## 14. Сохранения

- `SaveData { int Version; ProfileData; StashData; TradersData; QuestsData; StatsData }`. Настройки хранятся отдельным файлом.
- Сериализация — Newtonsoft JSON (`com.unity.nuget.newtonsoft-json`). DTO лежат в Core, преобразование модели в DTO и обратно — тоже в Core, с тестами «туда-обратно».
- Атомарная запись: запись во временный файл, `fsync`, переименование, копия `.bak`.
- Миграции — цепочка `ISaveMigration (from → to)`, каждая с тестом на образце старого сейва.
- Путь — `Application.persistentDataPath/profiles/<profileId>/`. Steam Cloud — в M9 или M12.

---

## 15. Рендер и отображение от первого лица

- URP, Forward+, SRP Batcher, GPU Resident Drawer (проверить выигрыш в M9), STP-апскейл для пресетов Low и Medium (особенно на Mac с Retina-экранами).
- Освещение: солнце в реальном времени с 2 каскадами теней на пресете Low (4 на High), запечённый GI через Adaptive Probe Volumes, минимум точечных источников в реальном времени. Несколько времён суток — позже, через Lighting Scenarios APV.
- Окклюзия: запечённая occlusion culling для зданий; GPU occlusion culling оцениваем в M9.
- FP-руки и оружие рисуются отдельным слоем через Renderer Feature (Render Objects) со своим FOV, чтобы не проваливаться в стены. Тело игрока — shadow-only для теней.
- Оптика: по умолчанию прицел через уменьшение FOV («дешёвый»), PiP через RenderTexture низкого разрешения — опция для High.
- Постобработка: тонмаппинг, цветокоррекция, лёгкая виньетка; на пресете Low без bloom и SSAO.

---

## 16. Сетевая готовность (правила с первого дня)

1. Состояние геймплея живёт в симуляции (Core и Gameplay), а не в визуальных объектах.
2. Любое изменение — через `CharacterInputFrame` или команду (`InventoryCommand` и др.).
3. Геймплей считается только в тике, никакой логики в `Update`.
4. Случайность — только через переданный `Rng` с seed, `UnityEngine.Random` в геймплее запрещён.
5. Gameplay не зависит от Presentation и UI.
6. У всего, что будет реплицироваться, есть `EntityId`.
7. Будущий define `RAIDLINE_SERVER` позволит вырезать Presentation и UI из серверной сборки.

Выбор сетевого стека (Netcode for GameObjects, Fish-Net, Photon Fusion 2 и др.) откладываем до M10 (T-1001).

---

## 17. Производительность (как добиваемся бюджетов)

- **Профилирование с первого дня:** `ProfilerMarker` в каждой системе тика, in-game HUD (время кадра, CPU, GPU, batches, GC через `ProfilerRecorder`), сцена `Benchmark` с выгрузкой CSV.
- **Горячие пути** (тик, баллистика, ИИ, UI-обновления в бою) — без аллокаций, LINQ, лямбд с замыканиями, `foreach` по интерфейсам, строковых операций, `GetComponent`, `Find`, `Camera.main` в цикле.
- **Пулы** для эффектов, декалей, звуков, трассеров и UI-элементов.
- **Burst и Jobs** для баллистики (батч raycast) и, при необходимости, для зрения ИИ.
- **Рендер:** палитра-атлас, один шейдер, LOD-группы, запечённый свет, ограниченные тени, окклюзия, GPU instancing растительности.
- **Анимация:** Animator culling, упрощённые аниматоры для Mid/Far-ботов.
- **Физика:** только нужные пары слоёв в матрице коллизий; маленькие предметы — без Rigidbody, пока их не бросили.
- **Звук:** лимит голосов (≈ 32 реальных), приоритеты, окклюзия лучами по бюджету.
- **Загрузка:** аддитивные сцены; Addressables для контента — в M8/M9.
- **Сборки:** IL2CPP для релиза, Managed Stripping Level — Medium (с `link.xml`), Incremental GC включён.
