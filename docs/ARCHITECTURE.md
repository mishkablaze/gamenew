# Архитектура Raidline

> Как устроен код и почему. Если реализация расходится с документом, то либо исправляется код, либо документ обновляется вместе с записью в `docs/DECISIONS.md`. Молча расходиться нельзя.

---

## 1. Принципы

1. **Ядро без движка.** Правила игры (инвентарь, урон, пробитие, экономика, сохранения) живут в сборке `Raidline.Core`, которая не ссылается на `UnityEngine`. Её можно тестировать через `dotnet test` без редактора, а позже запускать на сервере.
2. **Симуляция отдельно от отображения.** Геймплей никогда не зависит от эффектов, звука, анимаций и UI. Отображение подписывается на события симуляции.
3. **Один код для игрока и бота.** Персонаж — это набор компонентов, которым управляет источник ввода: игрок или мозг бота.
4. **Все изменения состояния — через команды.** UI и ввод ничего не меняют напрямую: они отправляют команду, авторитет (сейчас локальный, потом сервер) её проверяет и применяет.
5. **Фиксированный сетевой тик.** Геймплей считается в тике `TimeManager` Fish-Net (60 Гц), камера и рендер обновляются каждый кадр с интерполяцией.
6. **Ноль аллокаций в горячих путях.** Пулы, структуры, заранее выделенные буферы.
7. **Явные зависимости.** Без DI-фреймворков и без статических синглтонов с изменяемым состоянием. Сервисы создаёт composition root и передаёт явно.
8. **Сеть с первого дня (Fish-Net, D-017).** Сервер авторитетен; одиночная игра — режим хоста. Правила игры в Core о сети не знают.

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
| `Raidline.Gameplay` | Core, Infrastructure, Data, **FishNet.Runtime** | авторитетная симуляция, NetworkBehaviour-ы, `Gameplay/Networking` |
| `Raidline.AI` | Core, Infrastructure, Data, Gameplay, FishNet.Runtime | мозги ботов работают через `ICharacterInput`, только на сервере |
| `Raidline.Presentation` | Core, Infrastructure, Data, Gameplay, FishNet.Runtime | в серверной сборке не нужна |
| `Raidline.UI` | Core, Infrastructure, Data, Gameplay, FishNet.Runtime | читает состояние, отправляет команды |
| `Raidline.App` | все выше + FishNet.Runtime | Boot, NetworkManager, переходы, создание контекстов |
| `Raidline.Editor` | все + FishNet.Runtime | только `Editor` |

Core, Infrastructure и Data на Fish-Net **не ссылаются** (D-017).

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

- **`AppContext`** живёт всю сессию и создаётся `AppRoot`: `NetworkManager` (Fish-Net: TimeManager, ServerManager, ClientManager, транспорт Tugboat), `SessionLauncher` (офлайн = хост на 127.0.0.1), `TickRunner`, `UpdateRunner`, `InputService`, `SaveService`, `SettingsService`, `ItemDatabase`, `AudioService`, `SceneFlow`, `Log`.
- **`RaidContext`** живёт один рейд и создаётся `RaidRoot`. Серверная часть: `EntityRegistry`, `RaidEventBus`, `BallisticsSystem`, `NoiseBus`, `BotManager`, `LootSpawner`, `ExtractionSystem`, `RaidSession`, `Rng` (seed рейда). Клиентская часть: `RaidEventBus` для отображения, косметическая баллистика (трассеры), HUD. В режиме хоста в процессе есть обе.

MonoBehaviour-компоненты получают зависимости методом `Init(RaidContext ctx)` (или `Init(AppContext)`), который вызывает root или фабрика. `FindObjectOfType`, `GameObject.Find` и статические `Instance` для геймплейного состояния **запрещены**.

---

## 5. Время: тик и обновления

### Тик симуляции (Fish-Net, D-007, D-017)

- Источник тика — `TimeManager` Fish-Net: **60 Гц**, **Physics Mode = TimeManager** (физика шагает внутри сетевого тика, а не в `FixedUpdate`).
- `ITickSource` (Infrastructure) — абстракция «пришёл тик». Реализации:
  - `NetworkTickSource` (`Gameplay/Networking`) подписан на `TimeManager.OnTick` и передаёт номер тика и `dt`;
  - `LocalTickSource` (Infrastructure) крутит чистый `Raidline.Core.Time.TickClock` (аккумулятор `dt = 1/60`, не больше 4 тиков за кадр, `alpha`) — для EditMode/PlayMode-тестов и инструментов без сети.
- `TickRunner` (Infrastructure) по каждому тику источника вызывает `ITickable.Tick(in TickContext ctx)` **по фазам в фиксированном порядке**:

```
1. Input        — снимок ввода в CharacterInputFrame (игрок; у ботов — мозг на сервере)
2. Characters   — движение (Replicate/Reconcile), выносливость, поза
3. Weapons      — стрельба, перезарядка, отдача (выпуск снарядов)
4. Physics      — делает TimeManager (Physics Mode = TimeManager)
5. Ballistics   — перемещение снарядов, raycast-сегменты, попадания (сервер)
6. Damage       — применение урона, эффекты здоровья (сервер)
7. AI           — мозги ботов (сервер, по бюджету и LOD, §12)
8. Raid         — таймер, эвакуация, спавны (сервер)
```

- `TickContext` содержит номер тика, `dt` и флаги `IsServer`, `IsOwner`, `IsReplaying` (повтор тика при согласовании предсказания). Системы, у которых есть побочные эффекты вне предсказания (звук, эффекты, события UI), при `IsReplaying` их не выполняют.
- Ввод мыши и поворот камеры применяются **каждый кадр** (а не каждый тик), чтобы не было задержки ввода. В симуляцию передаётся итоговый угол на момент тика.
- Визуальные объекты сглаживаются между тиками средствами Fish-Net (графический объект персонажа отделён от сетевого корня).

### Обычные обновления

`UpdateRunner` — один MonoBehaviour на `Update`/`LateUpdate`, который вызывает зарегистрированные `IFrameUpdatable` и `ILateFrameUpdatable`. Компоненты регистрируются в `OnEnable` и снимаются в `OnDisable`. Свои `Update()` в сотнях компонентов не пишем.

**Геймплейная логика в `Update` / `LateUpdate` / `FixedUpdate` запрещена**: только в `ITickable` (или в Replicate-методе Fish-Net, который вызывается из тика). В `Update` допустимы отображение, UI и камера.

---

## 6. Сущности, события, команды

### Сущности

- Объекты, которые видят другие игроки (персонажи, предметы в мире, контейнеры, двери, выходы, трупы), — это `NetworkObject` Fish-Net; их сетевой ID — `NetworkObject.ObjectId`.
- `EntityId` — `readonly struct` с `int` для логики Core и событий; `EntityRegistry` (сервер) сопоставляет `EntityId ↔ NetworkObject`. Снаряды сетевыми объектами не являются (§9).

### События

- `EventChannel<T> where T : struct` — типизированный канал без упаковки (boxing) и без аллокаций при публикации. Подписка — делегатом.
- `RaidEventBus` держит каналы рейда (`ShotFired`, `ProjectileHit`, `DamageApplied`, `CharacterDied`, `ItemPickedUp`, `ExtractionProgress` …) и очищается в конце рейда. Серверные события, которые нужны клиентам для отображения, рассылаются ObserversRpc и на клиенте публикуются в тот же канал.
- UnityEvent в коде геймплея не используем.

### Команды и авторитет (сервер авторитетен)

| Что | Как идёт по сети |
|---|---|
| Движение, взгляд, кнопки персонажа | `CharacterInputFrame` → **Replicate** (предсказание на клиенте-владельце); состояние мотора → **Reconcile** |
| Инвентарь, лут, медицина, торговля | `InventoryCommand` → **ServerRpc** → сервер валидирует в Core и применяет → изменения владельцу (TargetRpc/SyncType) |
| Урон, смерть, эффекты здоровья | только сервер; клиентам — SyncVar/SyncType здоровья и события через ObserversRpc |
| Рейд: таймер, выходы, спавны | только сервер; клиентам — SyncVar/ObserversRpc |

- Персонаж получает `CharacterInputFrame` от `ICharacterInput`: `PlayerInputSource` (клиент-владелец) или `BotBrain` (сервер).
- `InventoryService.Execute(in InventoryCommand cmd)` выполняется **на сервере** и возвращает `InventoryResult`. UI клиента только отправляет команду и отображает результат.
- В режиме хоста всё это идёт тем же путём, просто внутри одного процесса.

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
Character (root, NetworkObject + EntityId)
├─ CharacterMotor        NetworkBehaviour: Replicate/Reconcile; CharacterController, поза, наклон, высота приседа, инерция
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

Выстрел проходит в два слоя. **Сервер** считает настоящие снаряды и попадания. **Клиент-владелец** сразу показывает вспышку, звук и косметический трассер (без ожидания сервера), остальные клиенты получают `ShotFired` через ObserversRpc и рисуют косметику у себя. Косметические снаряды попаданий не засчитывают.


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

- ИИ работает **только на сервере** (в режиме хоста — в том же процессе). Боты — сетевые персонажи без владельца-клиента.
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

## 16. Сеть (Fish-Net, D-017)

1. **Сервер авторитетен.** Боты, баллистика, урон, инвентарь, лут и рейд считаются на сервере; клиент шлёт ввод (Replicate) и команды (ServerRpc).
2. **Одиночная игра — режим хоста** (Tugboat на 127.0.0.1). Отдельного «офлайн-кода» нет.
3. **Правила — в Core**, без ссылок на Fish-Net: NetworkBehaviour-ы в Gameplay только переводят сетевые вызовы в вызовы Core.
4. Геймплей считается только в тике (`TimeManager`), никакой логики в `Update`.
5. Случайность — только через переданный `Rng` с seed; seed рейда знает сервер. `UnityEngine.Random` в геймплее запрещён.
6. Gameplay не зависит от Presentation и UI; Presentation и UI не меняют сетевое состояние напрямую.
7. Каждый Replicate-метод детерминирован относительно своего ввода и состояния (повторяется при согласовании).
8. В M10 серверная сборка (headless Linux) вырезает Presentation и UI (define `RAIDLINE_SERVER`; в Pro — автоматическое вырезание серверного кода из клиента).
9. Компенсация задержки (откат хитбоксов к моменту выстрела) — Fish-Net Pro, подключаем к PvP (T-1002). До этого попадания считаются по текущему состоянию сервера.

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
