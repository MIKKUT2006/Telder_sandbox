# Кровать 2x1 — V6

## Главное

Кровать — **один multi-block**, а не два отдельных блока. Для неё достаточно одной полной текстуры:

`Assets/GameData/ResourcePacks/Default/textures/multiblocks/bed.png`

Размер для 2x1: **32x16 px**. Левая половина картинки — первая клетка, правая — вторая.

В V6 отдельный `textures/blocks/bed.png` или `bed_item.png` больше не обязателен. Инвентарь берёт полную multi-block текстуру, а обычный BlockRenderer умеет взять 16x16 anchor-фрагмент только как аварийную совместимость со старыми сохранениями.

Пример `Blocks/bed.json`:

```json
{
  "ID": "game:bed",
  "Name": "Bed",
  "Texture": "bed",
  "Hardness": 1.0,
  "ExplosionResistance": 1.0,
  "Solid": false,
  "Transparent": true,
  "BlocksLight": false,
  "LightOpacity": 0,
  "Drop": "game:bed",
  "DropCount": 1,
  "Tags": ["bed", "furniture"],
  "MultiBlock": {
    "Width": 2,
    "Height": 1,
    "AnchorX": 0,
    "AnchorY": 0,
    "Kind": "bed",
    "Texture": "bed",
    "RequireFloor": true
  },
  "CollisionShape": "None",
  "CollisionRects": []
}
```

Пример `Items/bed.json`:

```json
{
  "ID": "game:bed",
  "Name": "Bed",
  "Texture": "bed",
  "Type": "Block",
  "MaxStack": 10
}
```

## Что исправлено в V6

Раньше `MultiBlockPlayerController` проверял `EventSystem.IsPointerOverGameObject()`. Из-за полноэкранных элементов HUD обычный клик по миру иногда ошибочно считался кликом по UI. В этот кадр multi-block обработчик пропускал кровать, а старый `BlockInteraction` ставил `game:bed` как обычный foreground-блок 1x1. Отсюда и плавающее поведение плюс `Texture missing: bed`.

Теперь:

- multi-block обработчик не зависит от raycast HUD;
- открытый настоящий InventoryUI всё равно блокирует взаимодействие с миром;
- `BlockInteraction` **вообще запрещено** ставить объявленный MultiBlock как обычный foreground-блок;
- старый furniture-controller тоже не может поставить MultiBlock как 1x1 мебель;
- `MultiBlockPlayerController` добавляется синхронно из `BlockInteraction.Start`, а bootstrap остаётся страховкой;
- если в старом save уже лежит ошибочный foreground-bed, его 16x16 anchor-фрагмент берётся из `multiblocks/bed.png`, поэтому break particles больше не вызывают `Texture missing: bed`. Такой старый блок достаточно один раз сломать и поставить заново.
