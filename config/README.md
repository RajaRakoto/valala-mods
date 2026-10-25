# Références de configuration Mjolnir

Ces fichiers ne sont **jamais déployés** ni inclus dans le package Thunderstore.

- `custom.cfg` : référence des valeurs livrées par défaut à tous les utilisateurs.
  Ces valeurs sont appliquées par le code ; le fichier n'est jamais déployé.
- `default.cfg` : copie de référence des valeurs par défaut de Mjolnir 2.6.4.

La seule configuration lue au runtime est :

```text
BepInEx/config/odin.valala.raja.mods.mjolnir.cfg
```

Les marges du HUD Mjolnir et les effets d'impact Valala sont dans la configuration
Valala :

```text
BepInEx/config/odin.valala.raja.mods.cfg
[Mjolnir HUD]
RightMargin = 12
BottomMargin = 72

[Mjolnir Impact]
Radius = 8
DamageAmount = 250
MinImpactSpeed = 8

```

Le menu Mjolnir est privé et n'est pas documenté publiquement.
