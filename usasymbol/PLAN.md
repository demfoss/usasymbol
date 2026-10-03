# usasymbol.com — план развития

> Обновлено: 2026-10-03. Основа — выгрузки Search Console (сентябрь 2026) + аудит репозитория.
> Контент: 648 рейтингов, 283 коллекции, 11 квизов, 40 ranking-hubs, weird-laws и weird-alcohol-laws на все 50 штатов.
> Сделанное не удаляется, а помечается ✅ / `[x]` с датой (чтобы не писать повторно), плюс запись в «Журнале» внизу.

---

## ▶ ОЧЕРЕДЬ РАБОТ — начинать отсюда

Главный вывод: сайт уже широкий. Рост — в **качестве существующих страниц**, а не в новых темах.

### 1. Переписать топ-страницы по промпту ranking.md (3–5 стр./нед.)

Критерий: много показов, позиция 7–15, текст тонкий и/или нет раздела «почему». `state-capitals-by-elevation` не трогать. Показы — выгрузка GSC «Страницы».

| # | Страница | Показы | Поз. | Заметка |
|---|---|---|---|---|
| 1 | ✅ geography/states-neighboring-states | 50 417 | 12,1 | сделано 2026-10-02 · CTR 0,19% |
| 2 | ✅ geography/states-with-no-snow | 31 271 | 8,4 | сделано 2026-10-02 |
| 3 | ✅ food/mcdonalds-by-state | 22 446 | 8,0 | сделано 2026-10-02 |
| 4 | ✅ taxes/vehicle-property-tax-by-state | 15 752 | 14,6 | сделано 2026-10-02 |
| 5 | ✅ law/drinking-age-by-state | 12 342 | 19,3 | сделано 2026-10-02 |
| 6 | ✅ geography/wild-west-states | 11 673 | 9,6 | сделано 2026-10-02 · CTR 0,34% |
| 7 | ✅ education/teacher-salary-by-state | 10 862 | 21,7 | сделано 2026-10-02 · шаблон jobs частично (нет OEWS-полей) |
| 8 | ✅ geography/rhode-islands-per-state | 10 371 | 7,0 | сделано 2026-10-02 |
| 9 | ✅ taxes/cigarette-prices-by-state | 7 780 | 11,6 | сделано 2026-10-02 |
| 10 | ✅ culture/state-fairs-by-state | 7 699 | 9,9 | сделано 2026-10-02 · точные цифры 2025 у 14 ярмарок |
| 11 | ✅ infrastructure/largest-airport-by-state | 7 300 | 9,4 | сделано 2026-10-02 · сохранён предварительный набор FAA CY2025 |
| 12 | ✅ law/alcohol-sales-laws | 6 693 | 11,3 | сделано 2026-10-02 · категориальная карта разрешена пользователем |
| 13 | ✅ law/front-license-plate-states | 6 452 | 11,4 | сделано 2026-10-02 · карта по числу пластин, исключения для отдельных регистраций |
| 14 | ✅ government/military-bases-by-state | 6 372 | 10,5 | сделано 2026-10-02 · National Guard и reserve вне исходного подсчёта |
| 15 | ✅ law/fireworks-laws-by-state | 6 194 | 11,0 | сделано 2026-10-02 · категориальная карта разрешена пользователем |
| 16 | ✅ education/smartest-states | 5 909 | 11,5 | сделано 2026-10-02 · индексы разделены, причины IQ-различий исходные данные не доказывают |
| 17 | ✅ law/age-of-consent-by-state | 5 577 | 14,9 | сделано 2026-10-02 · исправлена Arizona, полный аудит всех исключений не проводился |
| 18 | ✅ law/radar-detector-legality-by-state | 5 503 | 16,1 | сделано 2026-10-02 · категориальная карта разрешена пользователем |
| 19 | ✅ geography/lighthouses-by-state | 4 126 | 8,2 | сделано 2026-10-02 · высота включает lightning rod, даты сохранившихся башен |
| 20 | ✅ culture/casinos-by-state | 3 210 | 11,4 | сделано 2026-10-02 · удалены revenue и online-статистики вне таблицы |
| 21 | culture/zoos-by-state | 3 193 | 14,4 | |
| 22 | education/hardest-bar-exams-by-state | 2 643 | 14,5 | |
| 23 | agriculture/chicken-production-by-state | 2 297 | 8,5 | |
| 24 | sports/golf-courses-by-state | 2 078 | 11,7 | |

Длинные тире «—» в прозе (213 файлов) убирать при переписке, отдельной задачей не делать.
Дальше — остальные рейтинги без «Why» пачками, по убыванию показов.

### 2. Хабы

- [ ] Расширять хабы новыми страницами из списков ниже: Driving Laws (+1–5, 13, 14, 32), Guns & Weapons (+9, 10), Family & Marriage (+6, 7), Schools & Education (+28–31), Jobs & Wages (+15–27).

### 3. Grammy

- [ ] Осталось 15 штатов без уроженца с подтверждённой победой: Alaska, Colorado, Delaware, Idaho, Iowa, Maine, Montana, Nebraska, Nevada, New Hampshire, Rhode Island, South Dakota, Utah, Vermont, Wyoming. Искать артистов, сверять по grammy.com.

### 4. Ждут данных

- [ ] **Школы**: `states-by-k12-education` (24 075 показов, поз. 17,5) и `public-school-rankings-by-state` (14 761, поз. 15,6) — обе дают ~200 кликов. Решение после выгрузки GSC «Запросы» по обоим URL. От неё же зависят compare-метрики `k12-rank` / `public-school-rank` и место k12-rank в sitemap.
- [ ] **Ядро с позициями 20–60**: states-by-land-area (31 727, поз. 28,7), states-capital-cities (19 558, поз. 45), guides/state-abbreviations (20 881, поз. 58). Сначала запросы и конкурирующие URL в GSC.
- [ ] **Тест borders** (запущен 2026-10-02): у 10 страниц (colorado, kentucky, idaho, oklahoma, south-dakota, iowa, maryland, kansas, delaware, rhode-island) новый description без списка соседей, остальные 40 — контроль. Сравнить CTR в середине ноября. Если сработало — переписать остальные.

### 5. Новый контент — только реальные пробелы

- Списки ниже: «50 новых рейтингов» и вторая пачка.
- Уже есть (не писать): knife, butterfly knife, fireworks, window tint, squatters, one-party consent, rainwater, home alone, cousin marriage, car seat, helmet, raw milk, death tax, home/car insurance, minimum wage, right-to-work, квизы по столицам/флагам/формам/птицам/прозвищам, Walmart/Waffle House/Buc-ee's и др.

---

Статусы: `[ ]` не начато · `[~]` в работе · `[x]` готово (остаётся в плане с датой, запись — в журнал)

---

## 50 новых рейтингов (проверено: на сайте нет)

> Пачки с №101 — в [BACKLOG.md](BACKLOG.md). Проверка новых кандидатов: `node tools/check-backlog.js candidates.txt` из корня проекта (скрипт сверяет slug с Content/rankings, Content/collections, PLAN.md и BACKLOG.md).

Колонка «Почему интересно» — готовая причина для раздела «Why…» по новому промпту. Перед публикацией факты сверить с источником.

**Законы — в хабы Driving Laws / Guns & Weapons / Family & Marriage (14)**

| # | slug | Почему интересно |
|---|---|---|
| ~~1~~ | ~~law/right-on-red-laws-by-state~~ | ✅ 2026-10-02 · хабы Driving Laws + cars-and-roads |
| ~~2~~ | ~~law/open-container-laws-by-state~~ | ✅ 2026-10-02 · хабы Driving Laws, Alcohol + cars-and-roads |
| ~~3~~ | ~~law/lane-splitting-laws-by-state~~ | ✅ 2026-10-02 · хабы Driving Laws + cars-and-roads |
| 4 | law/move-over-laws-by-state | Есть во всех штатах, но сильно разные штрафы и охват ⚠️ Отложено 2026-10-02: нет проверяемой таблицы по 50 штатам (NCSL/AAA закрыты 403), нужны штрафы и охват disabled vehicles |
| ~~5~~ | ~~law/pets-in-hot-cars-laws-by-state~~ | ✅ 2026-10-02 · хабы Animals & Pets, Driving Laws + cars-and-roads |
| 6 | law/minor-curfew-laws-by-state | Штатные и городские комендантские часы для подростков ⚠️ Отложено 2026-10-02: нет данных по 50 штатам: комендантские часы в основном городские |
| ~~7~~ | ~~law/common-law-marriage-states~~ | ✅ 2026-10-02 · хаб Family & Marriage |
| 8 | law/lemon-laws-by-state | Сроки и пробег, после которых машину обязаны выкупить ⚠️ Отложено 2026-10-02: нет проверяемой таблицы: сводные таблицы с ошибками (Alabama, Colorado), нужна сверка 50 статутов |
| 9 | law/brass-knuckles-legality-by-state | Запрещены даже там, где разрешено огнестрельное оружие ⚠️ Отложено 2026-10-02: только SEO-блоги с противоречиями, нет надёжной таблицы |
| 10 | law/nunchucks-legality-by-state | Нью-Йорк запрещал до решения суда 2018 года ⚠️ Отложено 2026-10-02: запрещены только в Massachusetts, 49 строк «Legal» без числовой метрики для карты |
| 11 | law/metal-detecting-laws-by-state | Правила в парках и на пляжах, находки и археология ⚠️ Отложено 2026-10-02: правила парков и пляжей, нет таблицы по штатам и числовой метрики |
| 12 | law/roadkill-salvage-laws-by-state | Орегон в 2019 разрешил забирать сбитых оленей и лосей |
| 13 | law/golf-cart-street-legal-states | Пляжные и пенсионные городки Флориды |
| ~~14~~ | ~~law/bicycle-helmet-laws-by-state~~ | ✅ 2026-10-02 · хаб Driving Laws + cars-and-roads |

**Работа — хаб Jobs & Wages / «Salaries by State» (13)**

| # | slug | Почему интересно |
|---|---|---|
| ~~15~~ | ~~law/overtime-laws-by-state~~ | ✅ Написано 2026-10-02, DOL июль 2026, категориальная карта (числовая вводила бы в заблуждение). Kansas 46 ч — из закона штата, не из DOL |
| ~~16~~ | ~~law/paid-sick-leave-by-state~~ | ✅ Написано 2026-10-02, статус на октябрь 2026, карта по годовому лимиту часов |
| ~~17~~ | ~~law/meal-and-rest-break-laws-by-state~~ | ✅ Написано 2026-10-02, таблицы DOL от 1 янв 2023 |
| ~~18~~ | ~~law/minimum-age-to-work-by-state~~ | ✅ Написано 2026-10-02, DOL Employment/Age Certificate (янв 2024) + изменения штатов до 2026. Карта по возрасту, до которого нужен work permit; минимальный возраст 14 — федеральный |
| ~~19~~ | ~~economy/truck-driver-salary-by-state~~ | ✅ Написано 2026-10-02, OEWS May 2025 + RPP 2024, без «на 1 000 рабочих мест» |
| ~~20~~ | ~~economy/plumber-salary-by-state~~ | ✅ Написано 2026-10-02, OEWS May 2025 + RPP 2024 |
| ~~21~~ | ~~economy/dental-hygienist-salary-by-state~~ | ✅ Написано 2026-10-02, OEWS May 2025 |
| ~~22~~ | ~~economy/pharmacist-salary-by-state~~ | ✅ Написано 2026-10-02, OEWS May 2025 |
| ~~23~~ | ~~economy/nurse-practitioner-salary-by-state~~ | ✅ Написано 2026-10-02, OEWS May 2025 |
| ~~24~~ | ~~economy/software-developer-salary-by-state~~ | ✅ Написано 2026-10-02, OEWS May 2025 + RPP 2024, у Alaska зарплаты не опубликованы, без «на 1 000 рабочих мест» |
| ~~25~~ | ~~economy/hvac-technician-salary-by-state~~ | ✅ Написано 2026-10-02, OEWS May 2025 |
| ~~26~~ | ~~economy/welder-salary-by-state~~ | ✅ Написано 2026-10-02, OEWS May 2025 |
| ~~27~~ | ~~economy/physical-therapist-salary-by-state~~ | ✅ Написано 2026-10-02, OEWS May 2025 |

**Школа и подростки — хаб Schools & Education (5)**

| # | slug | Почему интересно |
|---|---|---|
| 28 | education/compulsory-school-age-by-state | Начало с 5 до 8 лет, окончание с 16 до 18 ⚠️ Отложено 2026-10-02: таблица NCES 2020 устарела (менялись Washington и др.), нужна свежая |
| 29 | education/corporal-punishment-in-schools-by-state | Всё ещё законны в части штатов, решение Верховного суда 1977 |
| 30 | education/school-cellphone-ban-states | Волна законов после Флориды (2023) |
| 31 | education/four-day-school-week-by-state | Популярна в сельских округах Колорадо, Миссури, Оклахомы |
| ~~32~~ | ~~law/minimum-driving-age-by-state~~ | ✅ 2026-10-02 · хаб Driving Laws, Cars & Roads + cars-and-roads |

**Энергия — новый хаб Energy (6)**

| # | slug | Почему интересно |
|---|---|---|
| 33 | infrastructure/oil-production-by-state | Техас (Пермский бассейн), Северная Дакота (Баккен) |
| 34 | infrastructure/natural-gas-production-by-state | Техас и Пенсильвания (сланец Marcellus) |
| 35 | infrastructure/coal-production-by-state | Вайоминг, бассейн Паудер-Ривер |
| 36 | infrastructure/solar-energy-by-state | Калифорния и быстрый рост Техаса |
| 37 | infrastructure/wind-energy-by-state | Техас первый по объёму, Айова — по доле |
| 38 | infrastructure/nuclear-power-plants-by-state | Иллинойс — больше всего реакторов |

**География и история — новый хаб Geography Facts (8)**

| # | slug | Почему интересно |
|---|---|---|
| 39 | geography/lowest-point-by-state | Долина Смерти −282 фута, Новый Орлеан ниже уровня моря |
| 40 | geography/longest-river-by-state | Миссури длиннее Миссисипи |
| 41 | geography/largest-lake-by-state | Большое Солёное озеро, Великие озёра |
| 42 | geography/tallest-waterfall-by-state | Йосемитский водопад |
| 43 | geography/caves-by-state | Мамонтова пещера — самая длинная система в мире |
| 44 | geography/civil-war-battles-by-state | Больше всего сражений было в Вирджинии |
| 45 | geography/national-historic-landmarks-by-state | Лидер — Нью-Йорк |
| 46 | geography/covered-bridges-by-state | Лидер — Пенсильвания |

**Госустройство — хаб Voting & Elections → «Government» (3)**

| # | slug | Почему интересно |
|---|---|---|
| ~~47~~ | ~~government/state-constitution-length-by-state~~ | ✅ 2026-10-02 · хаб Voting & Elections |
| ~~48~~ | ~~government/state-legislature-size-by-state~~ | ✅ 2026-10-02 · хаб Voting & Elections |
| ~~49~~ | ~~government/governor-salary-by-state~~ | ✅ 2026-10-02 · хаб Voting & Elections |

**Природа (1)**

| # | slug | Почему интересно |
|---|---|---|
| 50 | nature/endangered-species-by-state | Гавайи — больше всего видов под угрозой |

### Вторая пачка: ещё 50 (проверено: на сайте нет)

**Законы и право (12)**

| # | slug | Почему интересно |
|---|---|---|
| 51 | law/hitchhiking-laws-by-state | Во многих штатах разрешено, но не на автомагистралях |
| 52 | law/tattoo-age-laws-by-state | С согласием родителей в одних штатах можно раньше 18, в других нельзя никак |
| 53 | law/tanning-bed-age-laws-by-state | Калифорния первой запретила солярий до 18 лет (2011) |
| 54 | law/dog-bite-laws-by-state | Строгая ответственность против правила «одного укуса» |
| 55 | law/breed-specific-legislation-by-state | Часть штатов запрещает городам запрещать породы собак |
| 56 | law/tiny-house-laws-by-state | Где крошечный дом можно сделать постоянным жильём |
| 57 | law/truancy-laws-by-state | Техас до 2015 года вёл прогулы как правонарушение в уголовном суде |
| 58 | law/divorce-waiting-period-by-state | Калифорния требует 6 месяцев ожидания |
| 59 | law/small-claims-court-limits-by-state | Лимит отличается почти в 10 раз (Кентукки против Теннесси) |
| 60 | law/speeding-ticket-fines-by-state | В Вирджинии превышение на 20 миль/ч — уже reckless driving |
| 61 | taxes/car-registration-fees-by-state | Плата зависит от веса, возраста или цены машины |
| 62 | taxes/ev-registration-fees-by-state | Доплата за электромобиль вместо налога на бензин |

**Работа и деньги (8)**

| # | slug | Почему интересно |
|---|---|---|
| ~~63~~ | ~~economy/tipped-minimum-wage-by-state~~ | ✅ 2026-10-02 · хаб Jobs & Wages (Salaries by State) |
| 64 | economy/social-security-benefits-by-state | Средняя пенсия по штатам |
| 65 | economy/average-water-bill-by-state | Засушливый Запад против Великих озёр |
| 66 | economy/average-internet-bill-by-state | Сельские штаты платят больше за медленный интернет |
| 67 | education/college-tuition-by-state | Обучение для жителей штата в госвузах (проверить лидера) |
| 68 | economy/average-home-size-by-state | Площадь домов на Юге и в Новой Англии |
| 69 | government/state-debt-by-state | Долг на жителя |
| 70 | government/federal-funding-by-state | Кто получает от Вашингтона больше, чем платит |

**Школа (6)**

| # | slug | Почему интересно |
|---|---|---|
| 71 | education/naep-reading-scores-by-state | «Табель страны», Массачусетс традиционно в лидерах |
| 72 | education/naep-math-scores-by-state | То же по математике |
| 73 | education/universal-school-meals-states | Калифорния и Мэн первыми ввели бесплатные обеды для всех (2022) |
| 74 | education/teacher-shortage-by-state | Незаполненные вакансии учителей |
| 75 | education/universal-pre-k-by-state | Джорджия (1995) и Оклахома (1998) — пионеры |
| 76 | education/school-start-date-by-state | Юг начинает в начале августа, Северо-Восток после Дня труда |

**Здоровье (5)**

| # | slug | Почему интересно |
|---|---|---|
| 77 | health/air-quality-by-state | Лесные пожары и долины с инверсией |
| 78 | health/smoking-rate-by-state | Юта — самый низкий уровень курения |
| 79 | health/water-fluoridation-by-state | Юта (2025) первой запретила фторирование, затем Флорида |
| 80 | health/dentists-per-capita-by-state | Доступ к стоматологам |
| 81 | health/skin-cancer-rate-by-state | Юта лидирует по меланоме: высота и солнце |

**Погода, природа, география (12)**

| # | slug | Почему интересно |
|---|---|---|
| ~~82~~ | ~~geography/record-high-temperature-by-state~~ | ✅ 2026-10-02 · хаб Weather & Disasters |
| 83 | geography/record-low-temperature-by-state | Аляска, −80°F (Проспект-Крик, 1971) |
| 84 | geography/sunniest-states | Аризона и Юма |
| 85 | geography/cloudiest-states | Тихоокеанский Северо-Запад и Аляска |
| 86 | geography/average-elevation-by-state | Колорадо — самая высокая средняя высота, Делавэр — самая низкая |
| 87 | geography/forest-cover-by-state | Мэн — самый лесистый штат |
| 88 | geography/hot-springs-by-state | Айдахо (проверить лидера) |
| 89 | geography/sinkholes-by-state | Карстовые породы Флориды |
| 90 | nature/avalanche-deaths-by-state | Колорадо лидирует |
| 91 | geography/glaciers-by-state | Аляска; тающие ледники Монтаны |
| 92 | geography/state-parks-by-state | Аляска — больше всего площади госпарков |
| 93 | nature/national-wildlife-refuges-by-state | Заповедники для птиц на миграционных путях |

**История и люди (5)**

| # | slug | Почему интересно |
|---|---|---|
| 94 | geography/revolutionary-war-battles-by-state | Больше всего сражений было в Южной Каролине |
| 95 | geography/shipwrecks-by-state | Великие озёра и «Кладбище Атлантики» у Внешних отмелей |
| 96 | demographics/native-american-tribes-by-state | Аляска — 229 федерально признанных племён |
| 97 | demographics/foreign-born-population-by-state | Калифорния — больше четверти жителей |
| 98 | demographics/largest-ancestry-by-state | Немецкое происхождение — самое частое в большинстве штатов |

**Разное (2)**

| # | slug | Почему интересно |
|---|---|---|
| 99 | food/craft-distilleries-by-state | Бум после смягчения законов о дистилляции |
| 100 | government/governor-term-limits-by-state | В Вирджинии губернатор не может служить два срока подряд |

**Хабы для второй пачки:** Driving Laws (+51, 60–62), Family & Marriage (+58), Animals & Pets (+54, 55), Schools & Education (+57, 71–76), Jobs & Wages (+63), Cost of Living (+64–68), Government (+69, 70, 100), Health (+77–81), Weather & Disasters (+82–85, 89, 90), Geography Facts (+86–88, 91), Outdoors (+92, 93), новый хаб **History** (+94, 95 и существующие original-13-colonies, oldest-city, а также civil war, landmarks и covered bridges из первой пачки), Population & People (+96–98).

**Запас:** lyme-disease-cases, meteorite-falls, roller-coasters, museums-per-capita, libraries-per-capita, ghost-towns, tallest-state-capitol-buildings, wolf-population, bird-species-by-state, fourteeners-by-state, islands-by-state, hitchhiking-laws.

## Цель на год (до октября 2027)

**1 200–1 500 → 2 000–3 000 пользователей в день.**
- 2× за 12 мес. = ~6% роста в месяц; 2,5× = ~8%/мес. Базовый сценарий — 2,0–2,5 тыс., 3 тыс. — если зайдёт раздел jobs и появятся ссылки.
- Сравнивать год к году или с тем же периодом прошлого года: летом и в декабре будет провал (школьная аудитория).

Вклад рычагов (грубо, в пользователях/день к концу года):

| Рычаг | Прирост |
|---|---|
| CTR и позиции существующих страниц | +250–450 |
| Ядро: хабы штатов, символы, abbreviations, capitals | +150–400 |
| Нишевые законы (~60 страниц) | +150–300 |
| Salaries by State (~30 профессий) | +150–400 |
| Квизы и инструменты | +100–250 |
| Внешний трафик и ссылки | +50–200 |
| **Итого** | **+850–2 000** |

Темп: ~3–4 новые качественные страницы в неделю (~150–200 за год) + переработка ~50 существующих.

Промежуточные ориентиры (пользователей/день): январь 2027 — 1,5–1,7 тыс.; апрель — 1,8–2,0 тыс.; лето — просадка; октябрь 2027 — 2,0–3,0 тыс.

Риски: core-апдейты Google (особенно для шаблонных страниц), AI Overviews снижают CTR на фактовых запросах, сезонность.

## Срез на старте (сентябрь 2026)

> ⚠️ Файл `usasymbol.com_SearchPerformanceOverview_*.csv` по формату похож на выгрузку **Bing Webmaster Tools**, а не GSC. Уточнить разбивку трафика по источникам в GA4 (Acquisition → Traffic acquisition). Если Bing крупный — подключить IndexNow.

**Что работает** (масштабировать формат):
- Узкие рейтинги «X by state» с понятной цифрой: capitals-by-elevation (409 кликов, CTR 3,3%), nobel-prize (CTR 12%), goat-population, plastic-surgery, police-per-capita, gas-stations, oyster/wool/crawfish.
- Флаги с объектами: flags-with-animals (338 кликов), eagles (CTR 4,5%), people-figures.
- Нишевые законы: позиции 7–10, CTR 1–5% (alcohol-sales, sleeping-in-car, happy-hour, toplessness, lockpick, purple-paint, weird-laws-in-*).
- Фамилии по штатам (Rhode Island CTR 5,6%).
- Compare-пары по метрикам — CTR 2–30% на малых объёмах.

**Что не работает:**
- Хабы штатов `/states/{state}` — поз. 27–48.
- Ядровые списки символов: flags 41 · flowers 24 · birds 24 · trees 18,6 · nicknames 23 · mammals 27.
- Тривия по буквам/длине названий — сотни тысяч показов, CTR < 0,1% (zero-click).
- Массовые YMYL-законы: marijuana 81, abortion 60, marital-rape 54.
- Разделы most-dangerous-cities, counties, tools.

## Принятые решения

- ❌ **Не делаем** страницы «сколько точек сети X» (McDonald's, Target и т.п.) — голая цифра, zero-click. Существующие не трогаем. Исключение — сюжетные темы («Waffle House Index»).
- ❌ **Не делаем** новые страницы тривии по буквам/длине названий.
- ❌ **Не вкладываемся** в массовые YMYL-законы, counties, most-dangerous-cities, random-генераторы.
- ❌ **Не ставим** числа и ответы в title (проверено). Формат — «[Topic] | [Map, States, Facts]», год только «by State 2026» для ежегодных данных.
- ✅ Новый контент — только то, что нельзя ответить одной строкой в выдаче: объяснение, полная таблица + карта, свой составной рейтинг, инструмент/квиз.
- ✅ Новые серии запускаем пачкой 10 страниц → ждём 4–6 недель → смотрим GSC → масштабируем.

---

## Техника (по мере сил)

- [ ] Compare климат: `compare/climate/a-vs-b`, `/average-temperature`, `/summer-temperature`, `/winter-temperature` — один основной URL.
- [ ] Границы: `states-neighboring-states` / `guides/state-borders` / `states-by-border-type` — развести интенты или склеить.
- [ ] GSC → «Страницы»: проверить «Просканировано, не проиндексировано», особенно `/compare/`. Если compare раздут — убрать из sitemap пары без спроса, остальное noindex.
- [ ] Разбить sitemap по разделам: rankings, collections, compare, states, laws, quizzes, jobs.
- [ ] Разметка: `ItemList` (рейтинги), `Dataset` (таблицы), `FAQPage`, `BreadcrumbList`, `dateModified`.
- [ ] `max-image-preview:large` + OG-картинка-карта на каждом рейтинге.
- [ ] Регэксп-фильтры в GSC по разделам для ежемесячного сравнения.
- [ ] Compare-шаблон: title-вопрос («Is Texas Hotter Than Florida?») — сначала проверить, не противоречит ли правилу title без чисел.

## Ядро сайта: хабы штатов и символы

- [ ] **Хаб штата** `/states/{state}`: символы карточками, быстрые факты, соседи (ссылка на /borders), блок «Where [State] ranks» (топ‑5 / последние 5 из всех рейтингов), compare с соседями, фамилии, законы, цвета, зарплаты.
- [ ] **Списки символов** (flags, flowers, birds, trees, nicknames, mammals, mottos): таблица 50 штатов (символ, год, научное название, фото), FAQ, факты («7 штатов с Northern Cardinal»).
- [ ] **guides/state-abbreviations** — переписать: полная таблица (USPS + традиционные), printable, путаемые пары (MS/MO/MI/MA/ME…), квиз внизу. Отдельная страница «Confusing state abbreviations» (запрос «is ms mississippi»).
- [ ] **states-capital-cities** — таблица, printable, карта, квиз.
- [ ] Хабы категорий `/rankings`, `/rankings/law`, `/rankings/economy`, `/rankings/sports` — витрины с описанием и топ-страницами.
- [ ] Перелинковка: имя штата в рейтинге → хаб штата; хлебные крошки.

## Законы — то, чего нет в списках выше

- [ ] Sunday hunting / blue laws
- [ ] burial on private property
- [ ] swords, slingshots
- [ ] seat belt laws — переписать (поз. 61)

## Salaries by State — раздел jobs

**Источник:** BLS OEWS May 2025 + BEA Regional Price Parities. Не смешивать с Indeed/ZipRecruiter. ⚠️ Перед запуском проверить на bls.gov дату релиза.

**URL:** хаб уже есть — `/rankings/tag/jobs-wages` (H1 «Salaries by State»). Новые страницы — `/rankings/economy/{profession}-salary-by-state`. `teacher-salary-by-state` и `electrician-salary-by-state` — оставить URL, переделать по шаблону.

**Шаблон страницы:**
- [ ] Hero из данных: средняя по США, лидер/аутсайдер, разрыв в %, лидер после RPP
- [ ] Карта средней зарплаты
- [ ] Таблица: mean, median, hourly, employment (лишнее — в секции)
- [ ] Highest / lowest paying states
- [ ] **Salary adjusted for cost of living** (BEA RPP)
- [ ] **Licensing by state** — главное отличие от конкурентов
- [ ] Скрытые значения BLS (`**`, `#`) → «Data not available», не 0

**Техника:**
- [ ] Импортёр: OEWS xlsx + BEA RPP → фильтр по SOC → JSON
- [ ] Ежегодное обновление одним запуском (весна, после релиза OEWS)

**Пачка 1** — профессии из списка «Работа» (№19–27) + Registered Nurses, Police (переделать существующие). Пачка 2 (если пачка 1 держит топ‑10): LPN, nursing assistants, medical assistants, physician assistants, firefighters (существует), correctional officers, information security analysts, accountants. Пачка 3: paralegal, sonographer, radiologic technologist, respiratory therapist, carpenter, power-line installer, diesel mechanic, machinist, veterinarian, data scientist.

**Не делаем:** real estate agent (самозанятые), commercial driver (дубль truck driver), data analyst (нет SOC), solar installer / wind turbine tech / pilot / flight attendant (много скрытых значений), social worker одной страницей.

## Свои составные рейтинги и коллекции

- [ ] Best states for teachers (зарплата + RPP + class size)
- [ ] Best states for nurses (зарплата + RPP + Compact)
- [ ] Most expensive states to own a car (vehicle tax + страховка + бензин + inspections)
- [ ] Флаги: horses, trees, ships, mottos
- [ ] Переписать hottest-states (поз. 21,7)

## Квизы и инструменты

- [ ] State shapes quiz, blank map quiz («50 states no names»), state birds quiz, nicknames quiz, neighbors quiz — сначала проверить, каких нет среди 11 существующих
- [ ] Калькулятор «How many X fit in Y» (любая пара штатов) — развитие rhode-islands-per-state
- [ ] State Name Finder (буква/длина) — один инструмент вместо новых страниц тривии

## Внешний трафик и ссылки

- [ ] Карта-картинка для каждого рейтинга (OG + пост)
- [ ] Reddit: r/MapPorn, r/dataisbeautiful
- [ ] Pinterest: карты, флаги, printable-списки
- [ ] Рассылка местным СМИ штатов топ‑3 / последних 3 по каждому новому рейтингу
- [ ] Кнопка «Embed this map» с атрибуцией

## Свежесть и сезонность

- [ ] **Январь 2027:** 2026 → 2027 в заголовках, обновить данные и `dateModified`
- [ ] Весна 2027: обновить jobs из нового релиза OEWS
- Сезонный календарь (публиковать/обновлять за 4–6 недель): сентябрь–октябрь — снег, first snow, snow belt · январь–март — налоги · май — ураганы · июнь — state fairs

## Журнал

| Дата | Что сделано | Проверить |
|---|---|---|
| 2026-10-03 | Написаны 10 страниц по ranking.md: Arby's, Five Guys, Panda Express, Panera Bread, Sam's Club, IKEA, Best Buy, Whole Foods, U.S. Bank, PNC Bank. Таблицы, числовые карты, отдельные объяснения лидеров, 5–6 FAQ и внутренние ссылки. Добавлены в Restaurants & Fast Food и Store Locations, BACKLOG #413, 417–419, 475, 484, 486, 506, 538–539 закрыты. Данные официальных каталогов и FDIC, без вымышленных нулей. Panera — fiscal 2025 по транскрипции FDD, IKEA разделена по форматам, Best Buy включает Pacific Sales, Whole Foods — найденные страницы и подтверждённые открытия, Sam's Club — state facts с более ранними датами отчётности | Panera: сверить оригинал FDD при доступности. Whole Foods: дополнить неиндексированные магазины. Sam's Club: обновить даты state facts. Картинки и GSC через 4–6 недель |
| 2026-10-03 | Написаны 10 новых зарплатных страниц: dentist, psychologist, social-worker, correctional-officer, lawyer, paralegal, accountant, financial-analyst, cybersecurity-analyst, web-developer. В каждой 50 штатов, 4 раздела с таблицами, 7 FAQ и зелёная карта. Данные OEWS May 2025 и BEA RPP 2024, пропуски не заполнены вымышленными числами. Уточнены границы профессий, зарплаты сотрудников отделены от self-employment. Проверены цифры, ранги, SEO, YAML, денежные форматы и ссылки. Обновлены BACKLOG и jobs-wages | Проверить отображение после публикации |
| 2026-10-02 | Написаны 10 новых зарплатных страниц из BACKLOG №102–111: licensed-practical-nurse, nursing-assistant, medical-assistant, physician-assistant, radiologic-technologist, respiratory-therapist, occupational-therapist, speech-language-pathologist, veterinarian, veterinary-technician. В каждой 50 штатов, 4 раздела, 7 FAQ и зелёная числовая карта. OEWS May 2025 и BEA RPP 2024, поправка на цены и таблицы занятости. У veterinarian зарплаты Alaska и Delaware не опубликованы, employment сохранён. Добавлены в jobs-wages, YAML и числовые данные проверены | Проверить отображение после публикации |
| 2026-10-02 | Аудит по GSC, составлен план | — |
| 2026-10-02 | Склеены 5 дублей (301): bigfoot, state-fair-attendance, college-graduation-rates, flags-ranked-by-stars, newest-state-flags. Sitemap compare +10 метрик. 40 синих карт → teal, 17 длинных description сокращены | — |
| 2026-10-02 | Теги хабов: без тега было 137 рейтингов, осталось 4. Агро разбито на 6 групп. Всего 28 хабов с текстом (Salaries by State, 6 агро, Geography Facts, Weather, Health, Cost of Living, Cars & Roads, Outdoors, Sports, Pop Culture, Regions, Population) | индексация /rankings/tag/* |
| 2026-10-02 | Переписаны average-height, grammy-winners-by-state (номинации, grammy.com после 2026), states-with-stars-on-the-flag (Oregon 33) | CTR через 4–6 недель |
| 2026-10-02 | Исправлены факты: Bigfoot (Hawaii 0), Delaware borders (3 соседа, удалён мусорный текст), south-states (2-й по площади — Oklahoma), флаг Delaware, даты Texas на one-star flags | — |
| 2026-10-02 | Тест description на 10 borders-страницах | CTR в середине ноября |
| 2026-10-02 | +12 хабов (всего 40: Animals, Food, Business, Energy, Famous People, Religion, Gambling, Weird Laws, Military, Voting, Stores, Restaurants); Store Locations больше не тянет рестораны. Grammy расширен до 35 штатов | индексация /rankings/tag/* |
| 2026-10-02 | Новые законы по ranking.md: right-on-red, open-container, lane-splitting, pets-in-hot-cars, common-law-marriage (move-over отложен). Хабы driving-laws, alcohol, family-marriage, теги RankingTopicTags. Карты категориальные, depth standard — при следующей правке привести к строгому промпту (числовая карта, deep) | индексация, GSC через 4–6 недель |
| 2026-10-02 | Ещё 5 страниц строго по ranking.md (deep, числовая карта): bicycle-helmet-laws, minimum-driving-age, tipped-minimum-wage, state-legislature-size, record-high-temperature. Хабы driving-laws, jobs-wages, weather-disasters, voting-elections. Отложены #6, 8–11, 28, 47, 49 (нет проверяемых данных). economy/minimum-wage-by-state обновлена (см. ниже) | GSC через 4–6 недель |
| 2026-10-02 | Написаны governor-salary-by-state (данные 2025, IN и VT сверены с источниками штатов) и state-constitution-length-by-state (Book of the States через Wikipedia; у CT и IL длина пустая — одинаковые 16 401). Переписана economy/minimum-wage-by-state на данные Минтруда (июль 2026 + Florida $15), синхронизированы compare/stats/economy.yaml (20 штатов). Hero и карта-картинка у minimum-wage убраны (показывали 2025) | новые hero/карта для minimum-wage |
| 2026-10-02 | Переписан largest-airport-by-state на данных FAA CY2025 (было смешение посадок и пассажиропотока); новая коллекция collections/travel/largest-us-airlines (категория travel). 11 dofollow-ссылок на goairports.org (6 + 5). Промпты: collection.md выровнен с ranking.md, в оба добавлены partner_links | CTR и переходы на goairports через 4–6 недель |
| 2026-10-02 | Переписаны по ranking.md 10 топ-страниц: states-neighboring-states, states-with-no-snow, mcdonalds-by-state, vehicle-property-tax-by-state, drinking-age-by-state, wild-west-states, teacher-salary-by-state (зарплаты → числа + C0, без OEWS-полей), rhode-islands-per-state, cigarette-prices-by-state, state-fairs-by-state (14 ярмарок с точными цифрами 2025). Под вопросом данные: снегопад (Alaska < Vermont), vehicle tax (IN, AZ, NV, MI, OK, WA, OH), соседи NY/RI по воде, McDonald's — смесь 2021/2026 | CTR через 4–6 недель |
| 2026-10-02 | Параллельно переписаны следующие 10 страниц очереди, №11–20, по ranking.md. Добавлены объяснения, поисковые H2, FAQ и inline-ссылки, устранены цифры вне таблиц. Пользователь разрешил категориальные карты alcohol-sales-laws, fireworks-laws и radar-detector-legality. У smartest-states три метрики разделены, причинность IQ не установлена. Основные числовые ряды сохранены, юридические исключения не проходили полный аудит всех штатов | CTR через 4–6 недель |
| 2026-10-02 | Новые страницы по ranking.md (deep): законы overtime-laws, paid-sick-leave, meal-and-rest-break-laws, minimum-age-to-work; зарплаты truck-driver, plumber, software-developer (OEWS May 2025 + BEA RPP 2024). Добавлены в хаб jobs-wages (order) и теги Jobs & Wages. Без картинок. Нет колонки «на 1 000 рабочих мест» у truck/software (лимит API BLS) | картинки, Kansas в overtime, Virginia/WV/LA в sick-leave и work permits, GSC через 4–6 недель |
