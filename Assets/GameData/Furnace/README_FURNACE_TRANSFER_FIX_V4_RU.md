# Furnace transfer hotfix V4

## Причина бага

Проблема была не в Shift+ЛКМ и не в слотах печи.

`ItemFolderLoader` раньше считывал из JSON только:

- ID
- Name
- Texture
- Type
- MaxStack

Поля `Tags`, `FuelBurnTime`, `SmeltResult`, `SmeltResultCount` и `SmeltTime`
оставались только в JSON и **не попадали в ItemRegistry**.

Печь проверяет предметы через `ItemRegistry`, поэтому для неё:

- уголь имел `FuelBurnTime = 0` и не считался топливом;
- сырая руда имела `SmeltResult = null` и не считалась переплавляемой.

Из-за этого и Shift+ЛКМ, и перенос через курсор корректно отклоняли предметы.

## Что исправлено

`ItemFolderLoader` теперь загружает полную метаинформацию `ItemDefinition`:

- Tags
- EatTime
- HungerRestore
- HealthRestore
- FuelBurnTime
- SmeltResult
- SmeltResultCount
- SmeltTime
- CraftIngredients
- CraftWithoutWorkbench
- CraftResultCount

Старое значение `"Type": "items"` теперь интерпретируется как `Material`, а не
случайно как первый enum-тип `Block`.

После обновления перезапусти Play Mode/сцену, чтобы ItemRegistry загрузился заново.
