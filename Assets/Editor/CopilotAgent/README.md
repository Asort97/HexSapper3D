# Copilot Agent (Editor)

Набор утилит для удалённого редактирования сцены из файла команд.

- Файл команд: `Assets/Copilot/commands.json`
- Меню: Tools → Copilot Agent
  - Apply Commands Now — применить команды вручную
  - Toggle Auto Apply — включить/выключить авто-применение (пуллинг ~1 раз/сек)
  - Open Commands File — открыть или создать `commands.json`
  - Create Sample File — создать пример `commands.json.sample`

После успешного применения файл `commands.json` переименовывается в `commands.processed.yyyyMMdd_HHmmss.json`.

## Поддерживаемые команды

- InstantiatePrefab
  - prefabPath: string (путь к префабу в проекте)
  - name: string? (переопределить имя)
  - parent: string? (путь в иерархии, например `Canvas/MainMenu`)
  - position|localPosition|rotation|localEuler|scale
- CreateEmpty
  - name: string
  - parent: string?
  - position|localPosition|rotation|localEuler|scale
- SetTransform
  - target: string (путь к объекту в сцене)
  - position|localPosition|rotation|localEuler|scale
- SetRectTransform
  - target: string
  - anchoredPosition|sizeDelta|anchorMin|anchorMax|pivot
- AddComponent
  - target: string
  - component: string (имя типа, можно без пространства имён)
- SetProperty
  - target: string
  - component: string (тип компонента)
  - property: string (имя свойства/поля)
  - value: any ИЛИ valuePath+objectType ИЛИ assetPath+objectType

Пример:

```
{
  "commands": [
    {
      "type": "CreateEmpty",
      "name": "CoinsManager",
      "parent": "Canvas"
    },
    {
      "type": "AddComponent",
      "target": "Canvas/CoinsManager",
      "component": "CoinsManager"
    },
    {
      "type": "SetProperty",
      "target": "Canvas/CoinsManager",
      "component": "CoinsManager",
      "property": "coinsText",
      "valuePath": "Canvas/Gameplay/curr",
      "objectType": "TMPro.TextMeshProUGUI"
    }
  ]
}
```

Советы:
- Пути внутри сцены указываются через `/` от корня (root) объекта.
- При указании `component` можно писать полное имя типа (например, `TMPro.TextMeshProUGUI`) или простое имя, если тип уникален.
- Примитивы и векторы можно передавать массивами: `"position": [0,1,0]`, `"anchorMin": [0,0]`.
- Для надёжности используйте Undo: все операции регистрируются, сцену можно откатить Ctrl+Z.

Если установлен `HierarchySnapshot` с методом `ExportNow`, он будет вызван автоматически после применения команд.